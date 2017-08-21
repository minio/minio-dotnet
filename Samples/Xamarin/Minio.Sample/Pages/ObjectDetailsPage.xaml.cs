using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Minio.Sample.ViewModels;

namespace Minio.Sample.Pages
{
    public partial class ObjectDetailsPage : ViewPage<ObjectDetailsViewModel>
    {
        private ObjectDetailsPage()
        {
            InitializeComponent();
        }

        public ObjectDetailsPage(ObjectItem objectItem)
        : base(objectItem)
        {
            InitializeComponent();
        }
    }
}