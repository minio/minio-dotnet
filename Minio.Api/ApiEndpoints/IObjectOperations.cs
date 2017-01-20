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
        /*
         * To be implemented
     
        //accepts file instead of stream
        Task GetObjectAsync(string bucketName, string objectName,string filePath);

        Task PutObjectAsync(string bucketName, string objectName, Stream data, long size, string contentType,string fileName="");
        //accepts file instead of stream
        Task PutObjectAsync(string bucketName, string objectName, string filePath, string contentType);
        Task<ObjectStat> StatObjectAsync(string bucketName, string objectName);
        Task CopyObjectAsync(string bucketName, string objectName, string objectSource, CopyConditions conditions);
        Task RemoveObjectAsync(string bucketName, string objectName);
        //task RemoveObjects(string bucketName, Stream objectsList );
        Task RemoveIncompleteUpload(string bucketName, string objectName);
        */
    }
}
