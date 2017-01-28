using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testpolicy.MyPolicy
{
    class Statement
    {
        public string Sid { get; set; }
        public string Effect { get; set; }
        public Principal Principal { get; set; }
        public List<string> Action { get; set; }
        public List<string> Resource { get; set; }
        public ConditionMap conditions { get; set; }
       
    }
}
