using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Minio.DataModel;
using Xamarin.Forms;
using Minio.Sample.Pages;

namespace Minio.Sample.ViewModels
{
    public class BucketDetailsViewModel : BaseViewModel
    {
        private readonly IMinioClient minioClient;
        private ObservableCollection<ObjectItem> items;
        private string bucketName;
        bool isRefreshing;

        public BucketDetailsViewModel(IMinioClient minioClient)
        {
            this.minioClient = minioClient;
            this.RefreshCommand = new Command(DoRefreshCommand);
        }

        public override void Initialize(object parameter)
        {
            bucketName = (string)parameter;
            Title = bucketName;
            DoRefreshCommand();
        }

        public ObservableCollection<ObjectItem> Items
        {
            get => items;
            set
            {
                items = value;
                OnPropertyChanged();
            }
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

        public void OnTapped(object sender, ItemTappedEventArgs e)
        {
            Navigation.PushAsync(new ObjectDetailsPage((ObjectItem)e.Item));
        }

        protected async void DoRefreshCommand()
        {
            IsRefreshing = true;
            var itemsResult = await this.minioClient.ListObjectsAsync(this.bucketName);
            Items = new ObservableCollection<ObjectItem>(await ConvertItems(itemsResult));
            IsRefreshing = false;
        }

        protected async Task<IList<ObjectItem>> ConvertItems(IList<Item> itemsResult)
        {
            var result = new List<ObjectItem>();
            if (itemsResult != null)
            {
                foreach (var itemResult in itemsResult)
                {
                    var exp = (int)TimeSpan.FromDays(1).TotalSeconds;
                    var itemImageSource = await this.minioClient.PresignedGetObjectAsync(this.bucketName, itemResult.Key, exp);
                    result.Add(new ObjectItem(itemResult, itemImageSource));
                }
            }

            return result;
        }
    }

    public class ObjectItem
    {
        public Item Item { get; }

        public ObjectItem(Item item, string imageSource)
        {
            Item = item;
            ImageSource = imageSource;
        }

        public string Key => Item.Key;

        public string LastModified => Item.LastModified;

        public string ImageSource { get; }
    }
}