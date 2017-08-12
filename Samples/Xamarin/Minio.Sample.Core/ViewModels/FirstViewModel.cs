﻿using MvvmCross.Core.ViewModels;
using System.Threading.Tasks;
using MvvmCross.Platform.Platform;
using Minio;
using System;
using System.IO;

namespace Minio.Sample.Core.ViewModels
{
	using System.Text;

	public class FirstViewModel
		: MvxViewModel
	{
		private static Random rnd = new Random();

		string hello = "Hello MvvmCross";
		public string Hello
		{
			get { return hello; }
			set { SetProperty(ref hello, value); }
		}

		public IMvxCommand UploadFileCommand => new MvxAsyncCommand(DoUploadFileCommand);

		protected async Task DoUploadFileCommand()
		{
			var minioSettings = new MinioSettings(App.Minio.Endpoint, App.Minio.AccessKey, App.Minio.SecretKey)
			{
				CreateHttpClientHandlerFunc = ()=> new ModernHttpClient.NativeMessageHandler()
			};

			var minio = MinioClient.Create(minioSettings);
			minio.WithSsl();
			var getListBucketsTask =await minio.ListBucketsAsync();
			foreach (var item in getListBucketsTask.Buckets)
			{
				MvxTrace.Trace(item.Name);
			}

			var bucketName = "xamarin-" + Guid.NewGuid().ToString();

			await minio.MakeBucketAsync(bucketName);
			var found = await minio.BucketExistsAsync(bucketName);
			MvxTrace.Trace("bucket was " + found);

			var fileName = GetRandomName();
			var bytes = new byte[512];
			rnd.NextBytes(bytes);
			await minio.PutObjectAsync(bucketName,fileName, new MemoryStream(bytes), bytes.Length);
			MvxTrace.Trace($" BucketName: {bucketName}\nileName: {fileName}");

			await minio.RemoveObjectAsync(bucketName, fileName);
			await minio.RemoveBucketAsync(bucketName);
			MvxTrace.Trace($"Remove Bucket {bucketName}");
		}
		
		public static String GetRandomName()
		{
			var characters = "0123456789abcdefghijklmnopqrstuvwxyz";
			var result = new StringBuilder(5);
			for (int i = 0; i < 5; i++)
			{
				result.Append(characters[rnd.Next(characters.Length)]);
			}
			return "minio-dotnet-example-" + result;
		}
	}
}
