using System;
using System.Windows.Input;
using Xamarin.ExposureNotifications;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class ExposureDetailsViewModel : BaseViewModel
	{
		public ExposureDetailsViewModel(ExposureInfo info)
			=> ExposureInfo = info;

		public ICommand CancelCommand
			=> new Command(() => Navigation.PopModalAsync(true));

		public ExposureInfo ExposureInfo { get; set; }

		public DateTime When
			=> ExposureInfo.Timestamp;

		public TimeSpan Duration
			=> ExposureInfo.Duration;
	}
}
