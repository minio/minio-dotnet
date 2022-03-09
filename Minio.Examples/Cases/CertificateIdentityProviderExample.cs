// -*- coding: utf-8 -*-
// MinIO Python Library for Amazon S3 Compatible Cloud Storage,
// (C) 2022 MinIO, Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

using Minio.Credentials;
using Minio.DataModel;
using Minio.DataModel.Tracing;
using Newtonsoft.Json;


namespace Minio.Examples.Cases
{

    public class CeritificateIdentityProviderExample
    {
        // Establish Authentication on both ways with client and server certificates
        public async static Task Run()
        {
            // STS endpoint
            var stsEndpoint = "https://myminio:9000/";

            // Generatng pfx cert for this call.
            // openssl pkcs12 -export -out client.pfx -inkey client.key -in client.crt -certfile server.crt
            using (var cert = new X509Certificate2("C:\\dev\\client.pfx", "optional-password"))
            {
                var provider = new CertificateIdentityProvider()
                    .WithStsEndpoint(stsEndpoint)
                    .WithCertificate(cert)
                    .Build();

                MinioClient minioClient = new MinioClient()
                    .WithEndpoint("myminio:9000")
                    .WithSSL()
                    .WithCredentialsProvider(provider)
                    .Build();

                try
                {
                    StatObjectArgs statObjectArgs = new StatObjectArgs()
                    .WithBucket("bucket-name")
                    .WithObject("object-name");
                    ObjectStat result = await minioClient.StatObjectAsync(statObjectArgs);
                    Console.WriteLine("Object Stat: \n" + result.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"CertificateIdentityExample test exception: {e}");
                }
            }
        }
    }
}
