using System;
namespace Minio.Sample.ViewModels
{
    public class ObjectDetailsViewModel : BaseViewModel
    {
        private readonly IMinioClient minioClient;

        public ObjectItem Model { get; private set; }

        public ObjectDetailsViewModel(IMinioClient minioClient)
        {
            this.minioClient = minioClient;
        }

        public override void Initialize(object parameter)
        {
            base.Initialize(parameter);
            this.Model = (ObjectItem)parameter;
            Title = this.Model.Key;
        }
    }
}
