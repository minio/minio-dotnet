#I __SOURCE_DIRECTORY__
#r "../bin/Release/Minio.dll"

open Minio
open System

let minioClient = new MinioClient("https://play.minio.io:9000", "Q3AM3UQ867SPQQA43P2F",
                                  "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG")
minioClient.GetObject("/kline", "hello.txt",
                      Action<IO.Stream> (fun (stream : IO.Stream)->
                                             stream.CopyTo(Console.OpenStandardOutput())
                      ))