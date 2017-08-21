using System;
using System.ComponentModel;
using Xamarin.Forms;

namespace Minio.Sample.ViewModels
{
    public interface IViewModel : INotifyPropertyChanged
    {
        string Title { get; }

        void OnAppearing();

        void OnDisappearing();

        INavigation Navigation { get; set; }

        void Initialize(object parameter = null);
    }
}