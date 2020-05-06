using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using ExposureNotification.App.Services;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class ExposuresViewModel : BaseViewModel, IDisposable
	{
		public ExposuresViewModel()
		{
			MessagingCenter.Instance.Subscribe<ExposureNotificationHandler>(this, "exposure_info_changed", h =>
				Device.BeginInvokeOnMainThread(() =>
					{
						ExposureInformation.Clear();
						foreach (var i in LocalStateManager.Instance.ExposureInformation)
							ExposureInformation.Add(i);
					}));
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
			=> new ObservableCollection<Xamarin.ExposureNotifications.ExposureInfo>
			{
#if DEBUG
				new Xamarin.ExposureNotifications.ExposureInfo(DateTime.Now.AddDays(-7), TimeSpan.FromMinutes(30), 70, 6, Xamarin.ExposureNotifications.RiskLevel.High),
				new Xamarin.ExposureNotifications.ExposureInfo(DateTime.Now.AddDays(-3), TimeSpan.FromMinutes(10), 40, 3, Xamarin.ExposureNotifications.RiskLevel.Low),
#endif
			};

		public void Dispose()
			=> MessagingCenter.Instance.Unsubscribe<ExposureNotificationHandler>(this, "exposure_info_changed");
	}
}
