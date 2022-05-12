﻿/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
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

using System;
using System.Xml.Serialization;

namespace Minio.DataModel;

[Serializable]
[XmlRoot(ElementName = "ListMultipartUploadsResult", Namespace = "http://s3.amazonaws.com/doc/2006-03-01/")]
public class ListMultipartUploadsResult
{
    public string Bucket { get; set; }
    public string KeyMarker { get; set; }
    public string UploadIdMarker { get; set; }
    public string NextKeyMarker { get; set; }
    public string NextUploadIdMarker { get; set; }
    public int MaxUploads { get; set; }
    public bool IsTruncated { get; set; }
}