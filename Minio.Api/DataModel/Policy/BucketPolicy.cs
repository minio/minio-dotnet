using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using Minio.Policy;
using Minio.DataModel.Policy;

namespace Minio.DataModel
{
    public class BucketPolicy
    {
        [JsonIgnore]
        private string bucketName;
        [JsonProperty("Version")]
        private string version;

        [JsonProperty("Statement")]
        private List<Statement> statements { get; set; }

        public BucketPolicy(string bucketName=null)
        {
            if (bucketName != null)
            {
                this.bucketName = bucketName;
                this.version = "2012-10-17";
            }
        }
 
        
        /**
         * Reads JSON from given {@link Reader} and returns new {@link BucketPolicy} of given bucket name.
         */
        public static BucketPolicy parseJson(MemoryStream reader , String bucketName)
        {
            string toparse = new StreamReader(reader).ReadToEnd();
            JObject jsonData = JObject.Parse(toparse);

            BucketPolicy bucketPolicy = JsonConvert.DeserializeObject<BucketPolicy>(toparse);
            bucketPolicy.bucketName = bucketName;

            return bucketPolicy;
        }

        internal List<Statement> Statements()
        {
            return this.statements;
        }
      /**
        * Generates JSON of this BucketPolicy object.
        */
    //JsonIgnore
      public string getJson()  
      {
          return  JsonConvert.SerializeObject(this,Formatting.None, 
                            new JsonSerializerSettings { 
                                NullValueHandling = NullValueHandling.Ignore
            });
      }


    /**
     * Returns new bucket statements for given policy type.
     */
    private List<Statement> newBucketStatement(PolicyType policy, String prefix)
    {
        List<Statement> statements = new List<Statement>();

        if (policy == PolicyType.NONE || bucketName == null || bucketName.Length == 0)
        {
            return statements;
        }

        Resources resources = new Resources(Constants.AWS_RESOURCE_PREFIX + bucketName);

        Statement statement = new Statement();
        statement.actions = Constants.COMMON_BUCKET_ACTIONS;
        statement.effect = "Allow";
        statement.principal= new Principal("*");
        statement.resources = resources;
        statement.sid = "";

        statements.Add(statement);

        if (policy == PolicyType.READ_ONLY || policy == PolicyType.READ_WRITE)
        {
            statement = new Statement();
            statement.actions = Constants.READ_ONLY_BUCKET_ACTIONS;
            statement.effect =  "Allow";
            statement.principal = new Principal("*");
            statement.resources = resources;
            statement.sid = "";

            if (prefix != null && prefix.Length != 0)
            {
                ConditionKeyMap map = new ConditionKeyMap();
                map.put("s3:prefix", prefix);
                statement.conditions = new ConditionMap("StringEquals",map);
            }

            statements.Add(statement);
        }

        if (policy == PolicyType.WRITE_ONLY || policy == PolicyType.READ_WRITE)
        {
            statement = new Statement();
            statement.actions = Constants.WRITE_ONLY_BUCKET_ACTIONS;
            statement.effect = "Allow";
            statement.principal = new Principal("*");
            statement.resources = resources;
            statement.sid = "";

            statements.Add(statement);
        }

        return statements;
    }


    /**
     * Returns new object statements for given policy type.
     */
    private List<Statement> newObjectStatement(PolicyType policy, String prefix)
    {
        List<Statement> statements = new List<Statement>();

        if (policy == PolicyType.NONE || bucketName == null || bucketName.Length == 0)
        {
            return statements;
        }

        Resources resources = new Resources(Constants.AWS_RESOURCE_PREFIX + bucketName + "/" + prefix + "*");

        Statement statement = new Statement();
        statement.effect = "Allow";
        statement.principal = new Principal("*");
        statement.resources = resources;
        statement.sid = "";
        if (policy.Equals(PolicyType.READ_ONLY))
        {
            statement.actions = Constants.READ_ONLY_OBJECT_ACTIONS;
        }
        else if (policy.Equals(PolicyType.WRITE_ONLY))
        {
            statement.actions = Constants.WRITE_ONLY_OBJECT_ACTIONS;
        }
        else if (policy.Equals(PolicyType.READ_WRITE))
        {
            statement.actions = Constants.READ_WRITE_OBJECT_ACTIONS();
        }

        statements.Add(statement);
        return statements;
    }


    /**
     * Returns new statements for given policy type.
     */
    private List<Statement> newStatements(PolicyType policy, String prefix)
    {
        List<Statement> statements = this.newBucketStatement(policy, prefix);
        List<Statement> objectStatements = this.newObjectStatement(policy, prefix);

        statements.AddRange(objectStatements);

        return statements;
    }


    /**
     * Returns whether statements are used by other than given prefix statements.
     */
    //@JsonIgnore
    private bool[] getInUsePolicy(string prefix)
    {
        string resourcePrefix = Constants.AWS_RESOURCE_PREFIX + bucketName + "/";
        string objectResource = Constants.AWS_RESOURCE_PREFIX + bucketName + "/" + prefix + "*";

        bool readOnlyInUse = false;
        bool writeOnlyInUse = false;

        foreach( Statement statement in statements)
        {
            if (!statement.resources.Contains(objectResource)
                && statement.resources.startsWith(resourcePrefix).Count() != 0)
            {
                if (utils.isSupersetOf(statement.actions,Constants.READ_ONLY_OBJECT_ACTIONS))
                {
                    readOnlyInUse = true;
                }
                if (utils.isSupersetOf(statement.actions,Constants.WRITE_ONLY_OBJECT_ACTIONS))
                {
                    writeOnlyInUse = true;
                }
            }

            if (readOnlyInUse && writeOnlyInUse)
            {
                break;
            }
        }

        bool[] rv = { readOnlyInUse, writeOnlyInUse };
        return rv;
    }


    /**
     * Returns all statements of given prefix.
     */
    private void removeStatements(String prefix)
    {
        String bucketResource = Constants.AWS_RESOURCE_PREFIX + bucketName;
        String objectResource = Constants.AWS_RESOURCE_PREFIX + bucketName + "/" + prefix + "*";
        bool[] inUse = getInUsePolicy(prefix);
        bool readOnlyInUse = inUse[0];
        bool writeOnlyInUse = inUse[1];

        List<Statement> outList = new List<Statement>();
        ISet<String> s3PrefixValues = new HashSet<String>();
        List<Statement> readOnlyBucketStatements = new List<Statement>();

        foreach (Statement statement in statements)
        {
            if (!statement.isValid(bucketName))
            {
                outList.Add(statement);
                continue;
            }

            if (statement.resources.Contains(bucketResource))
            {
                if (statement.conditions != null)
                {
                    statement.removeBucketActions(prefix, bucketResource, false, false);
                }
                else
                {
                    statement.removeBucketActions(prefix, bucketResource, readOnlyInUse, writeOnlyInUse);
                }
            }
            else if (statement.resources.Contains(objectResource))
            {
                statement.removeObjectActions(objectResource);
            }

                if (statement.actions.Count != 0)
            {
                if (statement.resources.Contains(bucketResource)
                    && (utils.isSupersetOf(statement.actions,Constants.READ_ONLY_BUCKET_ACTIONS))
                    && statement.effect.Equals("Allow")
                    && statement.principal.aws().Contains("*"))
                {

                    if (statement.conditions != null)
                    {
                        ConditionKeyMap stringEqualsValue;
                        statement.conditions.TryGetValue("StringEquals",out stringEqualsValue);
                        if (stringEqualsValue != null)
                        {
                            ISet<string> values;
                            stringEqualsValue.TryGetValue("s3:prefix",out values);
                            if (values != null)
                            {
                                foreach(string v in values)
                                {
                                    s3PrefixValues.Add(bucketResource + "/" + v + "*");
                                }
                            }
                        }
                    }
                    else if (s3PrefixValues.Count() != 0)
                    {
                        readOnlyBucketStatements.Add(statement);
                        continue;
                    }
                }

             outList.Add(statement);
            }
        }

        bool skipBucketStatement = true;
        String resourcePrefix = Constants.AWS_RESOURCE_PREFIX + bucketName + "/";
        foreach (Statement statement in outList)
        {
            ISet<string> intersection = new HashSet<string>(s3PrefixValues);
            intersection.IntersectWith(statement.resources);

            if (statement.resources.startsWith(resourcePrefix).Count() != 0
                && intersection.Count() == 0)
            {
                skipBucketStatement = false;
                break;
            }
        }

        foreach (Statement statement in readOnlyBucketStatements)
        {
            IList<string> aws = statement.principal.aws();
            if (skipBucketStatement
                && statement.resources.Contains(bucketResource)
                && statement.effect.Equals("Allow")
                && aws != null && aws.Contains("*")
                && statement.conditions == null)
            {
                continue;
            }

      outList.Add(statement);
        }

        if (outList.Count() == 1) {
            Statement statement = outList[0];
            IList<string> aws = statement.principal.aws();
            if (statement.resources.Contains(bucketResource)
                && (utils.isSupersetOf(statement.actions,Constants.COMMON_BUCKET_ACTIONS))
                && statement.effect.Equals("Allow")
                && aws != null && aws.Contains("*")
                && statement.conditions == null)
            {
        outList = new List<Statement>();
            }
        }

        statements = outList;
    }


    /**
     * Appends given statement into statement list to have unique statements.
     * - If statement already exists in statement list, it ignores.
     * - If statement exists with different conditions, they are merged.
     * - Else the statement is appended to statement list.
     */
    private void appendStatement(Statement statement)
    {
        foreach (Statement s in statements)
        {
            IList<string> aws = s.principal.aws();
            ConditionMap conditions = s.conditions;

            if ((utils.isSupersetOf(s.actions,statement.actions)
                && s.effect.Equals(statement.effect)
                && aws != null && (utils.isSupersetOf(aws,statement.principal.aws()))
                && conditions != null && conditions.Equals(statement.conditions)))
            {
                s.resources.UnionWith(statement.resources);
                return;
            }

            if (s.resources.IsSupersetOf(statement.resources)
                && s.effect.Equals(statement.effect)
                && aws != null && (utils.isSupersetOf(aws,statement.principal.aws()))
                && conditions != null && conditions.Equals(statement.conditions))
            {
                s.actions.Union(statement.actions);
                return;
            }

            if (s.resources.IsSupersetOf(statement.resources)
                && (utils.isSupersetOf(s.actions,statement.actions)
                && s.effect.Equals(statement.effect)
                && aws != null && utils.isSupersetOf(aws,statement.principal.aws())))
            {
                if (conditions != null && conditions.Equals(statement.conditions))
                {
                    return;
                }

                if (conditions != null && statement.conditions!= null)
                {
                    conditions.putAll(statement.conditions);
                    return;
                }
            }
        }
        if (statement.actions != null && statement.resources != null && statement.actions.Count() != 0 && statement.resources.Count() != 0)
        {
            statements.Add(statement);
        }
    }


    /**
     * Appends new statements for given policy type.
     */
    private void appendStatements(PolicyType policy, String prefix)
    {
        List<Statement> appendStatements = newStatements(policy, prefix);
        foreach (Statement statement in appendStatements)
        {
            appendStatement(statement);
        }
    }


    /**
     * Returns policy type of this bucket policy.
     */
   // @JsonIgnore
  public PolicyType getPolicy(string prefix)
    {
        string bucketResource = Constants.AWS_RESOURCE_PREFIX + bucketName;
        string objectResource = Constants.AWS_RESOURCE_PREFIX + bucketName + "/" + prefix + "*";

        bool bucketCommonFound = false;
        bool bucketReadOnly = false;
        bool bucketWriteOnly = false;
        string matchedResource = "";
        bool objReadOnly = false;
        bool objWriteOnly = false;

        foreach (Statement s in statements)
        {
            ISet<string> matchedObjResources = new HashSet<string>();
            if (s.resources.Contains(objectResource))
            {
                matchedObjResources.Add(objectResource);
            }
            else
            {
                matchedObjResources = s.resources.match(objectResource);
            }

            if (matchedObjResources.Count() != 0)
            {
                bool[] rv = s.getObjectPolicy();
                bool readOnly = rv[0];
                bool writeOnly = rv[1];

                foreach (string resource in matchedObjResources)
                {
                    if (matchedResource.Length < resource.Length)
                    {
                        objReadOnly = readOnly;
                        objWriteOnly = writeOnly;
                        matchedResource = resource;
                    }
                    else if (matchedResource.Length == resource.Length)
                    {
                        objReadOnly = objReadOnly || readOnly;
                        objWriteOnly = objWriteOnly || writeOnly;
                        matchedResource = resource;
                    }
                }
            }
            else if (s.resources.Contains(bucketResource))
            {
                bool[] rv = s.getBucketPolicy(prefix);
                bool commonFound = rv[0];
                bool readOnly = rv[1];
                bool writeOnly = rv[2];
                bucketCommonFound = bucketCommonFound || commonFound;
                bucketReadOnly = bucketReadOnly || readOnly;
                bucketWriteOnly = bucketWriteOnly || writeOnly;
            }
        }

        if (bucketCommonFound)
        {
            if (bucketReadOnly && bucketWriteOnly && objReadOnly && objWriteOnly)
            {
                return PolicyType.READ_WRITE;
            }
            else if (bucketReadOnly && objReadOnly)
            {
                return PolicyType.READ_ONLY;
            }
            else if (bucketWriteOnly && objWriteOnly)
            {
                return PolicyType.WRITE_ONLY;
            }
        }

        return PolicyType.NONE;
    }


    /**
     * Returns policy type of all prefixes.
     */
    //@JsonIgnore
    public Dictionary<String, PolicyType> getPolicies()
    {
        Dictionary<String, PolicyType> policyRules = new Dictionary<string, PolicyType>();
        ISet<String> objResources = new HashSet<String>();

        String bucketResource = Constants.AWS_RESOURCE_PREFIX + bucketName;

        // Search all resources related to objects policy
        foreach (Statement s in statements)
        {
            objResources.UnionWith(s.resources.startsWith(bucketResource + "/"));
        }

        // Pretend that policy resource as an actual object and fetch its policy
        foreach (string r in objResources)
        {
            // Put trailing * if exists in asterisk
            string asterisk = "";
            string resource = r;
            if (r.EndsWith("*"))
            {
                resource = r.Substring(0, r.Length - 1);
                asterisk = "*";
            }

            String objectPath = resource.Substring(bucketResource.Length + 1, resource.Length);
            PolicyType policy = this.getPolicy(objectPath);
            policyRules.Add(bucketName + "/" + objectPath + asterisk, policy);
        }

        return policyRules;
    }


    /**
     * Sets policy type for given prefix.
     */
   // @JsonIgnore
    public void setPolicy(PolicyType policy, String prefix)
    {
        if (statements == null)
        {
            statements = new List<Statement>();
        }

        removeStatements(prefix);
        appendStatements(policy, prefix);
    }
  }
}
