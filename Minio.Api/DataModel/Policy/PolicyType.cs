using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.DataModel
{
  
    public class  PolicyType
    {
        private PolicyType(string value) { Value = value; }
        public string Value { get; set; }

        public static PolicyType NONE {  get { return new PolicyType("none"); } }
        public static PolicyType READ_ONLY { get { return new PolicyType("readonly"); } }
        public static PolicyType READ_WRITE { get { return new PolicyType("readwrite"); } }
        public static PolicyType WRITE_ONLY { get { return new PolicyType("writeonly"); } }

    }

}
