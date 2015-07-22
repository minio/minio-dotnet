using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Minio.Errors
{
    public class DataSizeMismatchException : ClientException
    {
        public string Bucket { get; set; }
        public string Key { get; set; }
        public long UserSpecifiedSize { get; set; }
        public long ActualReadSize { get; set; }
    }
}
