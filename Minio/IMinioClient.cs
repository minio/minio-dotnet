/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
 * (C) 2020 MinIO, Inc.
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

using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Encryption;
using Minio.DataModel.ILM;
using Minio.DataModel.Notification;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Replication;
using Minio.DataModel.Response;
using Minio.DataModel.Result;
using Minio.DataModel.Select;
using Minio.DataModel.Tags;
using Minio.Exceptions;
using Minio.Handlers;

namespace Minio;

public interface IMinioClient : IBucketOperations, IObjectOperations, IDisposable
{
    void SetAppInfo(string appName, string appVersion);
    void SetTraceOff();
    void SetTraceOn(IRequestLogger logger = null);
    Task<HttpResponseMessage> WrapperGetAsync(Uri uri);
    Task WrapperPutAsync(Uri uri, StreamContent strm);
}
