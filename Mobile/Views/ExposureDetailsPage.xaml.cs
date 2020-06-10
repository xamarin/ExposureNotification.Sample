using Mobile.ViewModels;
using Xamarin.ExposureNotifications;
using Xamarin.Forms;

namespace Mobile.Views
{
	public partial class ExposureDetailsPage : ContentPage
	{
		public ExposureDetailsPage()
		{
			InitializeComponent();

			BindingContext = new ExposureDetailsViewModel();
		}
	}
}
