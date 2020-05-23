using System;
using System.Collections.ObjectModel;
using ExposureNotification.App.Services;
using ExposureNotification.App.Views;
using MvvmHelpers.Commands;
using Newtonsoft.Json;
using Xamarin.ExposureNotifications;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class ExposuresViewModel : ViewModelBase
	{
		public ExposuresViewModel()
        {

        }

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

		bool navigating;

		public AsyncCommand<ExposureInfo> ExposureSelectedCommand => new AsyncCommand<ExposureInfo>((info) =>
		   GoToAsync($"{nameof(ExposureDetailsPage)}?info={JsonConvert.SerializeObject(info)}"));
	}
}
