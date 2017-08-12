using Android.OS;
using Android.Support.V7.Widget;
using MvvmCross.Droid.Support.V7.AppCompat;

namespace Minio.Sample.Droid.Views
{
	public abstract class BaseView : MvxAppCompatActivity
	{
		protected Toolbar Toolbar { get; set; }

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			SetContentView(LayoutResource);

			Toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
			if (Toolbar != null)
			{
				SetSupportActionBar(Toolbar);
				SupportActionBar.SetDisplayHomeAsUpEnabled(true);
				SupportActionBar.SetHomeButtonEnabled(true);
			}
		}

		protected abstract int LayoutResource { get; }
	}
}
