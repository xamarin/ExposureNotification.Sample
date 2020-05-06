using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ExposureNotification.App.ViewModels
{
	public class ExposuresViewModel : BaseViewModel
	{
		public ObservableCollection<Xamarin.ExposureNotifications.ExposureInfo> ExposureInformation
			=> new ObservableCollection<Xamarin.ExposureNotifications.ExposureInfo>
			{
#if DEBUG
				new Xamarin.ExposureNotifications.ExposureInfo(DateTime.Now.AddDays(-7), TimeSpan.FromMinutes(30), 70, 6, Xamarin.ExposureNotifications.RiskLevel.High),
				new Xamarin.ExposureNotifications.ExposureInfo(DateTime.Now.AddDays(-3), TimeSpan.FromMinutes(10), 40, 3, Xamarin.ExposureNotifications.RiskLevel.Low),
#endif
			};
		
	}
}
