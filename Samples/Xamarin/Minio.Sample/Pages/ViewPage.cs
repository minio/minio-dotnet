using System;
using Xamarin.Forms;
using System.ComponentModel;
using Autofac;
using Minio.Sample.ViewModels;

namespace Minio.Sample.Pages
{
    public class ViewPage<T> : ContentPage where T : IViewModel
    {
        readonly T viewModel;

        public T ViewModel => viewModel;

        public ViewPage(object parameter = null)
        {
            using (var scope = AppContainer.Container.BeginLifetimeScope())
            {
                viewModel = AppContainer.Container.Resolve<T>();
                viewModel.Initialize(parameter);
                viewModel.Navigation = this.Navigation;
            }

            BindingContext = viewModel;
            Title = viewModel.Title;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ViewModel.OnDisappearing();
        }
    }
}