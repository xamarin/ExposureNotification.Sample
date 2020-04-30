using ExposureNotification.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.App
{
	public class ExposureNotificationHandler : IExposureNotificationHandler
	{
		public Configuration Configuration
			=> new Configuration();

		public async Task ExposureStateUpdated()
		{
			// Fetch details
			var summary = await Xamarin.ExposureNotifications.ExposureNotification.GetExposureSummary();

			if (summary != null && summary.MatchedKeyCount > 0)
			{
				// Some detected, get the details
				var details = await Xamarin.ExposureNotifications.ExposureNotification.GetExposureInformation();

				// TODO: Save
			}

			// TODO: Send notification alerting the user
		}

		public async Task RequestDiagnosisKeys()
		{
			var keys = await Xamarin.ExposureNotifications.ExposureNotification.GetTemporaryExposureKeys();

			X509Certificate2 cert = null;

			using (var s = Assembly.GetCallingAssembly().GetManifestResourceStream(Config.CertificateResourceFilename))
			using (var m = new MemoryStream())
			{
				await s.CopyToAsync(m);
				m.Position = 0;

				cert = new X509Certificate2(m.ToArray());
			}

			var client = new ExposureNotificationWebClient(
				Config.ApiUrlBase,
				new DefaultTemporaryExposureKeyEncoder(cert));

			await client.SubmitPositiveDiagnosisAsync(keys);
		}

	}
}
