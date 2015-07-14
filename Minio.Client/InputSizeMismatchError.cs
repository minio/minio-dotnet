using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Minio.Client
{
    public class InputSizeMismatchError : Exception
    {
        public string Bucket { get; set; }
        public string Key { get; set; }
        public long UserSpecifiedSize { get; set; }
        public long ActualReadSize { get; set; }
    }
}
