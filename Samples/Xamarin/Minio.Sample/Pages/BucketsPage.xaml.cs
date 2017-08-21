using Minio.Sample.ViewModels;

namespace Minio.Sample.Pages
{
    public partial class BucketsPage : ViewPage<BucketsViewModel>
    {
        public BucketsPage()
        {
            InitializeComponent();
            this.ListView.ItemTapped += this.ViewModel.OnTapped;
        }
    }
}
