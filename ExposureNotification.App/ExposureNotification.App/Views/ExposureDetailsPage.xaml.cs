using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExposureNotification.App.ViewModels;
using Xamarin.ExposureNotifications;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ExposureNotification.App.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ExposureDetailsPage : ContentPage
	{
		public ExposureDetailsPage(ExposureInfo exposureInfo)
		{
			InitializeComponent();

			BindingContext = new ExposureDetailsViewModel(exposureInfo);
		}
	}
}