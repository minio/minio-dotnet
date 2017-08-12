using MvvmCross.Platform.IoC;

namespace Minio.Sample.Core
{
	public class App : MvvmCross.Core.ViewModels.MvxApplication
	{
		public static class Minio 
		{
			public const string AccessKey = "Q3AM3UQ867SPQQA43P2F";

			public const string SecretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";

			public const string Endpoint ="play.minio.io:9000";
		}

		public override void Initialize()
		{
			CreatableTypes()
				.EndingWith("Service")
				.AsInterfaces()
				.RegisterAsLazySingleton();

			RegisterAppStart<ViewModels.FirstViewModel>();
		}
	}
}