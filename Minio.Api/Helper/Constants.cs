using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Helper
{
   static class Constants
    {
        // Maximum number of parts.
        public static int MaxParts = 10000;
        // Minimum part size.
        public static long MinimumPartSize = 5 * 1024L * 1024L;
        // Maximum part size.
        public static long MaximumPartSize = 5 * 1024L * 1024L * 1024L;
        // Maximum streaming object size.
        public static long MaximumStreamObjectSize = MaxParts * MinimumPartSize;
        // maxSinglePutObjectSize - maximum size 5GiB of object per PUT
        // operation.
        public static long MaxSinglePutObjectSize = 1024L * 1024L * 1024L * 5;

        // maxMultipartPutObjectSize - maximum size 5TiB of object for
        // Multipart operation.
        public static long MaxMultipartPutObjectSize = 1024L * 1024L * 1024L * 1024L * 5;

        // optimalReadBufferSize - optimal buffer 5MiB used for reading
        // through Read operation.
        public static long OptimalReadBufferSize = 1024L * 1024L * 5;


    }
}
