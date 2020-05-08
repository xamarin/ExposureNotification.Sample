using System;
using System.Collections.Generic;
using System.Linq;
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

		public static async Task StartAsync()
			=> await PlatformStart();

		public static async Task StopAsync()
			=> await PlatformStop();

		public static Task<bool> IsEnabledAsync()
			=> PlatformIsEnabled();

		// Call this when the user has confirmed diagnosis
		public static async Task SubmitSelfDiagnosisAsync()
		{
			var selfKeys = await GetSelfTemporaryExposureKeysAsync();

			await Handler.UploadSelfExposureKeysToServerAsync(selfKeys);
		}

		// Call this when the app needs to update the local keys
		public static async Task<bool> UpdateKeysFromServer()
		{
			var keys = await Handler?.FetchExposureKeysFromServerAsync();

			if (keys?.Any() != true)
				return false;

			var (summary, info) = await DetectExposuresAsync(keys);

			// Check that the summary has any matches before notifying the callback
			if (summary?.MatchedKeyCount > 0)
				await Handler.ExposureDetectedAsync(summary, info);

			return true;
		}

		internal static Task<(ExposureDetectionSummary, IEnumerable<ExposureInfo>)> DetectExposuresAsync(IEnumerable<TemporaryExposureKey> diagnosisKeys)
			=> PlatformDetectExposuresAsync(diagnosisKeys);

		internal static Task<IEnumerable<TemporaryExposureKey>> GetSelfTemporaryExposureKeysAsync()
			=> PlatformGetTemporaryExposureKeys();
	}

	public class Configuration
	{
		// Minimum risk score required to record
		public int MinimumRiskScore { get; set; } = 4;

		public int AttenuationWeight { get; set; } = 50;

		public int TransmissionWeight { get; set; } = 50;

		public int DurationWeight { get; set; } = 50;

		public int DaysSinceLastExposureWeight { get; set; } = 50;

		public int[] TransmissionRiskScores { get; set; } = new int[] { 4, 4, 4, 4, 4, 4, 4, 4 };

		// Scores assigned to the attenuation of the BTLE signal of exposures
		// A > 73dBm, 73 <= A > 63, 63 <= A > 51, 51 <= A > 33, 33 <= A > 27, 27 <= A > 15, 15 <= A > 10, A <= 10
		public int[] AttenuationScores { get; set; } = new[] { 4, 4, 4, 4, 4, 4, 4, 4 };

		// Scores assigned to each length of exposure
		// < 5min, 5min, 10min, 15min, 20min, 25min, 30min, > 30min
		public int[] DurationScores { get; set; } = new[] { 4, 4, 4, 4, 4, 4, 4, 4 };

		// Scores assigned to each range of days of exposure
		// >= 14days, 13-12, 11-10, 9-8, 7-6, 5-4, 3-2, 1-0
		public int[] DaysSinceLastExposureScores { get; set; } = new[] { 4, 4, 4, 4, 4, 4, 4, 4 };
	}
}
