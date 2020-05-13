using System;
using System.Collections.ObjectModel;
using ExposureNotification.App.Services;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class ExposuresViewModel : BaseViewModel
	{
		public bool EnableNotifications
		{
			get => LocalStateManager.Instance.EnableNotifications;
			set
			{
				LocalStateManager.Instance.EnableNotifications = value;
				LocalStateManager.Save();
			}
		}

		public ObservableCollection<Xamarin.ExposureNotifications.ExposureInfo> ExposureInformation
			=> LocalStateManager.Instance.ExposureInformation;
	}
}
