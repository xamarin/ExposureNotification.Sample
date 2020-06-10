using System.ComponentModel;
using Mobile.ViewModels;
using Xamarin.Forms;

namespace Mobile.Views
{
	public partial class NotifyOthersPage : ContentPage
	{
		public NotifyOthersPage()
		{
			InitializeComponent();
		}

		protected override void OnAppearing()
		{
			if (BindingContext is ViewModelBase vm)
				vm.NotifyAllProperties();

			base.OnAppearing();
		}
	}
}
