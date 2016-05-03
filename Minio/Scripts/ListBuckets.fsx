#I __SOURCE_DIRECTORY__
#r "../bin/Release/Minio.dll"

open Minio
open System

/// N B: Listing buckets on minio server at play.minio.io test access-key and secret-key
let mc = new MinioClient("https://play.minio.io:9000", "Q3AM3UQ867SPQQA43P2F",  "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG")
mc.ListBuckets() |> Seq.iter (fun b -> printfn "%s" b.Name)