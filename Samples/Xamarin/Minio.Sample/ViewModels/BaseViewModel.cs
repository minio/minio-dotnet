using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace Minio.Sample.ViewModels
{
    public abstract class BaseViewModel : IViewModel
    {
        private string title;

        public event PropertyChangedEventHandler PropertyChanged;

        public INavigation Navigation { get; set; }

        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Title
        {
            get => title;
            set
            {
                title = value;
                OnPropertyChanged();
            }
        }

        public virtual void OnAppearing()
        {

        }

        public virtual void OnDisappearing()
        {

        }

        public virtual void Initialize(object parameter)
        {

        }
    }
}