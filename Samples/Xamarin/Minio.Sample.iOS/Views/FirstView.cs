using System;
using Minio.Sample.Core.ViewModels;
using MvvmCross.Binding.BindingContext;
using MvvmCross.iOS.Views;

namespace Minio.Sample.iOS.Views
{
	[MvxFromStoryboard]
	public partial class FirstView : MvxViewController
	{
		public FirstView(IntPtr handle) : base(handle)
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			var set = this.CreateBindingSet<FirstView, FirstViewModel>();
			set.Bind(Label).To(vm => vm.Hello);
			set.Bind(TextField).To(vm => vm.Hello);
			set.Bind(UploadButton).To(vm => vm.UploadFileCommand);
			set.Apply();
		}
	}
}
