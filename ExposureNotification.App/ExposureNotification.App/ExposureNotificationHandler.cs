using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xamarin.Essentials;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.App
{
	public class ExposureNotificationHandler : IExposureNotificationHandler
	{
		public void OnContactsDetected(IEnumerable<ContactInfo> contacts)
		{
			// Save contacts so the user can view them later

			// Send notification
		}

		public async void ShouldSubmitTemporaryExposureKeys(List<Xamarin.ExposureNotifications.TemporaryExposureKey> keys)
		{
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
