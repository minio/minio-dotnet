using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Client.xml
{
    [Serializable]
    public class Part
    {
        private string etag;
        public int PartNumber { get; set; }

        public string ETag
        {
            get
            {
                return etag;
            }
            set
            {
                if (value != null)
                {
                    etag = value.Replace("\"", "");
                }
                else
                {
                    etag = null;
                }
            }
        }
    }
}
