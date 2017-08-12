// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//

using System.CodeDom.Compiler;
using Foundation;

namespace Minio.Sample.iOS.Views
{
	[Register("FirstView")]
	partial class FirstView
	{
		[Outlet]
		[GeneratedCode("iOS Designer", "1.0")]
		UIKit.UILabel Label { get; set; }

		[Outlet]
		[GeneratedCode("iOS Designer", "1.0")]
		UIKit.UITextField TextField { get; set; }

		[Outlet]
		[GeneratedCode("iOS Designer", "1.0")]
		UIKit.UIButton UploadButton {get;set; }

		void ReleaseDesignerOutlets()
		{
			if (Label != null)
			{
				Label.Dispose();
				Label = null;
			}

			if (TextField != null)
			{
				TextField.Dispose();
				TextField = null;
			}
		}
	}
}
