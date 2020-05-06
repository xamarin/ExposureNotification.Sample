using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using ExposureNotification.App.Resources;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class SharePositiveDiagnosisViewModel : BaseViewModel
	{
		public ICommand CancelCommand
			=> new Command(async () =>
				await Navigation.PopModalAsync(true));
	}
}
