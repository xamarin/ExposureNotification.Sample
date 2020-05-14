using System.ComponentModel;
using System.Threading.Tasks;
using MvvmHelpers;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class ViewModelBase : BaseViewModel
	{
		public void NotifyAllProperties()
			=> OnPropertyChanged(null);

		bool isEnabled;
		public bool IsEnabled
        {
			get => isEnabled;
			set => SetProperty(ref isEnabled, value);
        }

		bool navigating;
		public async Task GoToAsync(string path)
        {
			if (navigating)
				return;
			navigating = true;

			await Shell.Current.GoToAsync(path);

			navigating = false;
        }

	}
}
