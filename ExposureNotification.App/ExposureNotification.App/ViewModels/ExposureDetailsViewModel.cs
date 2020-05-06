using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Newtonsoft.Json;
using Xamarin.ExposureNotifications;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class ExposureDetailsViewModel : BaseViewModel
	{
		public ExposureDetailsViewModel(ExposureInfo info)
			=> ExposureInfo = info;

		public ICommand CancelCommand
			=> new Command(async () =>
				await Navigation.PopModalAsync(true));

		public ExposureInfo ExposureInfo { get; set; }
	}
}
