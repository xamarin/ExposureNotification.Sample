using System.Linq;
using Xamarin.ExposureNotifications;
using Xamarin.Forms;

namespace ExposureNotification.App.Views
{
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
