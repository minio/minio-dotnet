using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Minio.DataModel;
using System.Windows.Input;
using Xamarin.Forms;
using Minio.Sample.Pages;

namespace Minio.Sample.ViewModels
{
    public class BucketsViewModel : BaseViewModel
    {
        private readonly IMinioClient minioClient;
        private ObservableCollection<Bucket> buckets;
        bool isRefreshing;

        public BucketsViewModel(IMinioClient minioClient)
        {
            this.minioClient = minioClient;
            this.RefreshCommand = new Command(DoRefreshCommand);
        }

        public ICommand RefreshCommand { get; }

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Bucket> Buckets
        {
            get => buckets;
            set
            {
                buckets = value;
                OnPropertyChanged();
            }
        }

        public override void Initialize(object parameter)
        {
            base.Initialize(parameter);
            DoRefreshCommand();
            Title = "Buckets";
        }

        public void OnTapped(object sender, ItemTappedEventArgs e)
        {
            Navigation.PushAsync(new BucketDetailsPage((e.Item as Bucket)?.Name));
        }

        protected async void DoRefreshCommand()
        {
            IsRefreshing = true;
            var bucketsResult = await this.minioClient.ListBucketsAsync();
            Buckets = new ObservableCollection<Bucket>(bucketsResult.Buckets);
            IsRefreshing = false;
        }

    }
}