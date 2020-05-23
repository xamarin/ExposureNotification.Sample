﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExposureNotification.App.Services;
using ExposureNotification.App.ViewModels;
using ExposureNotification.App.Views;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ExposureNotification.App
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AppShell : Shell
	{
		public AppShell()
		{
			InitializeComponent();

#if DEBUG
			tabDeveloper.IsEnabled = true;
#endif

			Routing.RegisterRoute(nameof(ExposureDetailsPage), typeof(ExposureDetailsPage));
			Routing.RegisterRoute(nameof(SharePositiveDiagnosisPage), typeof(SharePositiveDiagnosisPage));

			if (LocalStateManager.Instance.LastIsEnabled)
				GoToAsync($"//{nameof(InfoPage)}", false);

		}
	}
}