using System.ComponentModel;
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

	}
}
