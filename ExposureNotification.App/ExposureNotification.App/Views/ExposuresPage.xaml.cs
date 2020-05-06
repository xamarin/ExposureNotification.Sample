using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xamarin.ExposureNotifications;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ExposureNotification.App.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ExposuresPage : ContentPage
	{
		public ExposuresPage()
		{
			InitializeComponent();
		}

		async void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var info = e.CurrentSelection.FirstOrDefault();

			if (info != null && info is ExposureInfo exposureInfo)
				await Navigation.PushModalAsync(new NavigationPage(new ExposureDetailsPage(exposureInfo)), true);
			
			(sender as CollectionView).SelectedItem = null;
		}
	}
}