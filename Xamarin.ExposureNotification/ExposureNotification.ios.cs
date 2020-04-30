using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ExposureNotifications;
using System.Linq;

namespace Xamarin.ExposureNotifications
{
	public static partial class ExposureNotification
	{
		static ENManager manager;
		static ENExposureDetectionSession session;
		

		static async Task<ENManager> GetManagerAsync()
		{
			if (manager == null)
			{
				manager = new ENManager();
				await manager.ActivateAsync();
			}

			return manager;
		}

		static async Task<ENExposureDetectionSession> GetSessionAsync()
		{
			if (session == null)
			{
				session = new ENExposureDetectionSession();
				await session.ActivateAsync();
			}

			return session;
		}

		static async Task PlatformStart(IExposureNotificationHandler handler)
		{
			var m = await GetManagerAsync();
			await m.SetExposureNotificationEnabledAsync(true);
		}

		static async Task PlatformStop()
		{
			var m = await GetManagerAsync();
			await m.SetExposureNotificationEnabledAsync(false);
		}

		static async Task<bool> PlatformIsEnabled()
		{
			var m = await GetManagerAsync();
			return m.ExposureNotificationEnabled;
		}

		// Gets the contact info of anyone the user had contact with who was diagnosed
		static async Task<IEnumerable<ExposureInfo>> PlatformGetExposureInformation()
		{
			return null;
			//var s = await GetSessionAsync();
			//s.
			//s.GetExposureInfoAsync();
		}

		static async Task<ExposureDetectionSummary> PlatformGetExposureSummary()
		{
			return null;
			//var s = await GetSessionAsync();
			//s.AddDiagnosisKeysAsync()
		}

		// Call this when the user has confirmed diagnosis
		static async Task PlatformSubmitPositiveDiagnosis()
		{	
			var m = await GetManagerAsync();
			var selfKeys = await m.GetDiagnosisKeysAsync();

			await ExposureNotification.Handler.SubmitSelfDiagnosisKeysToServer(
				selfKeys.Select(k => new TemporaryExposureKey(
					k.KeyData.ToArray(),
					k.RollingStartNumber,
					TimeSpan.FromMinutes(k.RollingStartNumber),
					(RiskLevel)k.TransmissionRiskLevel)));
		}

		// Tells the local API when new diagnosis keys have been obtained from the server
		static async Task PlatformProcessDiagnosisKeys(IEnumerable<TemporaryExposureKey> diagnosisKeys)
		{
			var s = await GetSessionAsync();

			await s.AddDiagnosisKeysAsync(diagnosisKeys.Select(k =>
				new ENTemporaryExposureKey
				{
					KeyData = NSData.FromArray(k.KeyData),
					RollingStartNumber = (uint)k.RollingStart
				}).ToArray());
		}
	}
}
