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

using System.Security.Cryptography.X509Certificates;
using Minio.Credentials;

namespace Minio.Examples.Cases;

public static class CertificateIdentityProviderExample
{
    // Establish Authentication on both ways with client and server certificates
    public static async Task Run()
    {
        // STS endpoint
        var stsEndpoint = "https://alias:port/";

        // Generatng pfx cert for this call.
        // openssl pkcs12 -export -out client.pfx -inkey client.key -in client.crt -certfile server.crt
        using var cert = new X509Certificate2("C:\\dev\\client.pfx", "optional-password");
        try
        {
            var provider = new CertificateIdentityProvider()
                .WithStsEndpoint(stsEndpoint)
                .WithCertificate(cert)
                .Build();

            using var minioClient = new MinioClient()
                .WithEndpoint("alias:port")
                .WithSSL()
                .WithCredentialsProvider(provider)
                .Build();

            var statObjectArgs = new StatObjectArgs()
                .WithBucket("bucket-name")
                .WithObject("object-name");
            var result = await minioClient.StatObjectAsync(statObjectArgs).ConfigureAwait(false);
            // Console.WriteLine("\nObject Stat: \n" + result.ToString());
            Console.WriteLine("\nCertificateIdentityProvider test PASSed\n");
        }
        catch (Exception e)
        {
            Console.WriteLine($"\nCertificateIdentityProvider test exception: {e}\n");
        }
    }
}