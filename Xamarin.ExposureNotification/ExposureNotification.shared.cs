using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.ExposureNotifications
{
	public static partial class ExposureNotification
	{
		static IExposureNotificationHandler handler;

		internal static IExposureNotificationHandler Handler
		{
			get
			{
				if (handler != default)
					return handler;

				// Look up implementations of IExposureNotificationHandler
				var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (var asm in allAssemblies)
				{
					if (asm.IsDynamic)
						continue;

					var asmName = asm.GetName().Name;

					if (asmName.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
						|| asmName.StartsWith("Xamarin.", StringComparison.OrdinalIgnoreCase))
						continue;

					var allTypes = asm.GetExportedTypes();

					foreach (var t in allTypes)
					{
						if (t.IsClass && t.IsAssignableFrom(typeof(IExposureNotificationHandler)))
							handler = (IExposureNotificationHandler)Activator.CreateInstance(t.GetType());
					}
				}

				if (handler == default)
					throw new NotImplementedException($"Missing an implementation for {nameof(IExposureNotificationHandler)}");

				return handler;
			}
		}

		public static Task<bool> IsEnabledAsync()
			=> PlatformIsEnabled();

		public static async Task StartAsync()
			=> await PlatformStart(Handler);

		public static async Task StopAsync()
			=> await PlatformStop();

		// Gets the contact info of anyone the user had contact with who was diagnosed
		internal static Task<IEnumerable<ExposureInfo>> GetExposureInformationAsync()
			=> PlatformGetExposureInformation();

		// Tells the local API when new diagnosis keys have been obtained from the server
		internal static Task<ExposureDetectionSummary> AddDiagnosisKeysAsync(IEnumerable<TemporaryExposureKey> diagnosisKeys)
			=> PlatformAddDiagnosisKeys(diagnosisKeys);

		public static Task SubmitSelfDiagnosisAsync()
			=> PlatformSubmitSelfDiagnosis();

		internal static Task<IEnumerable<TemporaryExposureKey>> GetSelfTemporaryExposureKeysAsync()
			=> PlatformGetTemporaryExposureKeys();

		public static async Task<bool> UpdateKeysFromServer()
		{
			var keys = await Handler?.FetchExposureKeysFromServer();

			var updates = keys != null && keys.Any();

			if (updates)
			{
				var summary = await AddDiagnosisKeysAsync(keys);

				// Check that the summary has any matches before notifying the callback
				if (summary != null && summary.MatchedKeyCount > 0)
				{
					// This will run and invoke the handler in the background to deal with results
					_ = Task.Run(() =>
						  Handler.ExposureDetected(summary,
							  () => GetExposureInformationAsync()));
				}
			}

			return updates;
		}
	}

	public class Configuration
	{
		// Minimum risk score required to record
		public int MinimumRiskScore { get; set; } = 1;

		public int[] TransmissionRiskScores { get; set; } = new int[] { 1, 1, 1, 1, 1, 1, 1, 1 };

		// Scores assigned to the attenuation of the BTLE signal of exposures
		// A > 73dBm, 73 <= A > 63, 63 <= A > 51, 51 <= A > 33, 33 <= A > 27, 27 <= A > 15, 15 <= A > 10, A <= 10
		public int[] AttenuationScores { get; set; } = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

		// Scores assigned to each length of exposure
		// < 5min, 5min, 10min, 15min, 20min, 25min, 30min, > 30min
		public int[] DurationScores { get; set; } = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

		// Scores assigned to each range of days of exposure
		// >= 14days, 13-12, 11-10, 9-8, 7-6, 5-4, 3-2, 1-0
		public int[] DaysScores { get; set; } = new[] { 8, 7, 6, 5, 4, 3, 2, 1 };
	}
}
