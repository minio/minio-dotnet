/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2021 MinIO, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Xml.Serialization;

/*
 * LifecycleRule is used within LifecycleConfiguration as an encapsulation of rules.
 * Please refer:
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_PutBucketLifecycleConfiguration.html
 * https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetBucketLifecycleConfiguration.html
 */

namespace Minio.DataModel.ILM;

[Serializable]
[XmlRoot(ElementName = "Rule")]
public class LifecycleRule
{
    public static readonly string LIFECYCLE_RULE_STATUS_ENABLED = "Enabled";
    public static readonly string LIFECYCLE_RULE_STATUS_DISABLED = "Disabled";

    private RuleFilter _ruleFilter;

    public LifecycleRule()
    {
        _ruleFilter = new RuleFilter();
    }

    public LifecycleRule(AbortIncompleteMultipartUpload abortIncompleteMultipartUpload, string id,
        Expiration expiration, Transition transition, RuleFilter filter,
        NoncurrentVersionExpiration noncurrentVersionExpiration,
        NoncurrentVersionTransition noncurrentVersionTransition,
        string status)
    {
        if (!status.Equals(LIFECYCLE_RULE_STATUS_ENABLED) && !status.Equals(LIFECYCLE_RULE_STATUS_DISABLED))
            throw new ArgumentException("Wrong value assignment for " + nameof(Status));
        AbortIncompleteMultipartUploadObject = abortIncompleteMultipartUpload;
        ID = id;
        Expiration = expiration;
        TransitionObject = transition;
        Filter = filter;
        NoncurrentVersionExpirationObject = noncurrentVersionExpiration;
        NoncurrentVersionTransitionObject = noncurrentVersionTransition;
        Status = status;
    }

    [XmlElement(ElementName = "AbortIncompleteMultipartUpload", IsNullable = true)]
    public AbortIncompleteMultipartUpload AbortIncompleteMultipartUploadObject { get; set; }

    [XmlElement(ElementName = "ID")] public string ID { get; set; }

    [XmlElement(ElementName = "Expiration", IsNullable = true)]
    public Expiration Expiration { get; set; }

    [XmlElement(ElementName = "Transition", IsNullable = true)]
    public Transition TransitionObject { get; set; }

    [XmlElement("Filter", IsNullable = true)]
    public RuleFilter Filter
    {
        get => _ruleFilter;
        set
        {
            // The filter must not be missing, even if it is empty.
            if (value == null)
                _ruleFilter = new RuleFilter();
            else
                _ruleFilter = value;
        }
    }

    [XmlElement("NoncurrentVersionExpiration", IsNullable = true)]
    public NoncurrentVersionExpiration NoncurrentVersionExpirationObject { get; set; }

    [XmlElement("NoncurrentVersionTransition", IsNullable = true)]
    public NoncurrentVersionTransition NoncurrentVersionTransitionObject { get; set; }

    [XmlElement("Status")] public string Status { get; set; }
}