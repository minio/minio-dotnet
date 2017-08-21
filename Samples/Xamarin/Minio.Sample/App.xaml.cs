using Autofac;
using Xamarin.Forms;
using Minio.Sample.ViewModels;
using Minio.Sample.Pages;

namespace Minio.Sample
{
    public partial class App : Application
    {
        public static class Minio
        {
            public const string AccessKey = "Q3AM3UQ867SPQQA43P2F";

            public const string SecretKey = "zuf+tfteSlswRu7BJ86wekitnifILbZam1KYY3TG";

            public const string Endpoint = "play.minio.io:9000";
        }

        public App()
        {
            InitializeComponent();
            AppContainer.Container = CreateContainer();
            MainPage = new NavigationPage(new BucketsPage());
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        public IContainer CreateContainer()
        {
            var containerBuilder = new ContainerBuilder();
            RegisterDependencies(containerBuilder);
            return containerBuilder.Build();
        }

        protected virtual void RegisterDependencies(ContainerBuilder cb)
        {
            var minioSettings = new MinioSettings(App.Minio.Endpoint, App.Minio.AccessKey, App.Minio.SecretKey)
            {
                CreateHttpClientHandlerFunc = () => new ModernHttpClient.NativeMessageHandler()
            };

            cb.Register(c => MinioClient.Create(minioSettings).WithSsl()).As<IMinioClient>();
            cb.RegisterType<BucketsViewModel>().SingleInstance();
            cb.RegisterType<BucketDetailsViewModel>().As<BucketDetailsViewModel>();
            cb.RegisterType<ObjectDetailsViewModel>().As<ObjectDetailsViewModel>();
        }
    }
}