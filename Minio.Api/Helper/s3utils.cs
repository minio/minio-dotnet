using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Minio.Helper
{
    class s3utils
    {

        internal static bool isAmazonEndPoint(Uri uri)
        {
            if (isAmazonChinaEndPoint(uri))
            {
                return true;
            }
            return uri.Host == "s3.amazonaws.com";
        }
        // IsAmazonChinaEndpoint - Match if it is exactly Amazon S3 China endpoint.
        // Customers who wish to use the new Beijing Region are required
        // to sign up for a separate set of account credentials unique to
        // the China (Beijing) Region. Customers with existing AWS credentials
        // will not be able to access resources in the new Region, and vice versa.
        // For more info https://aws.amazon.com/about-aws/whats-new/2013/12/18/announcing-the-aws-china-beijing-region/
        internal static bool isAmazonChinaEndPoint(Uri uri)
        {

            return uri.Host == "s3.cn-north-1.amazonaws.com.cn";
        }
        // IsGoogleEndpoint - Match if it is exactly Google cloud storage endpoint.
        internal static bool isGoogleEndPoint(Uri endpointUri)
        {
            return endpointUri.Host == "storage.googleapis.com";
        }
        internal static string GetPath(string p1, string p2)
        {
            try
            {
                string combination = Path.Combine(p1, p2);
                combination = Uri.EscapeUriString(combination);
                return combination;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }

        }
    }
}
