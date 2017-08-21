using Minio.Sample.ViewModels;

namespace Minio.Sample.Pages
{
    public partial class BucketDetailsPage : ViewPage<BucketDetailsViewModel>
    {
        public BucketDetailsPage(string bucketName)
            : base(bucketName)
        {
            InitializeComponent();
            this.ListView.ItemTapped += ViewModel.OnTapped;
        }
    }
}
