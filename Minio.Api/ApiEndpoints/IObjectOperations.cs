using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minio.DataModel;
using RestSharp;
namespace Minio
{
    public interface IObjectOperations
    {
        Task GetObjectAsync(string bucketName, string objectName, Action<Stream> callback);
        Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType);
        Task RemoveObjectAsync(string bucketName, string objectName);
        Task<ObjectStat> StatObjectAsync(string bucketName, string objectName);
        IObservable<Upload> ListIncompleteUploads(string bucketName, string prefix, bool recursive);
        Task RemoveIncompleteUploadAsync(string bucketName, string objectName);
        Task<CopyObjectResult> CopyObjectAsync(string bucketName, string objectName, string destBucketName, string destObjectName = null, CopyConditions copyConditions = null);

        /*
         * To be implemented
        //accepts file instead of stream
       //  Task PutObjectAsync(string bucketName, string objectName, string filePath, string contentType);

         Task GetObjectAsync(string bucketName, string objectName, string filePath, string contentType);

        //accepts file instead of stream
        Task CopyObjectAsync(string bucketName, string objectName, string objectSource, CopyConditions conditions);
        //task RemoveObjects(string bucketName, Stream objectsList );
        */
    }
}
