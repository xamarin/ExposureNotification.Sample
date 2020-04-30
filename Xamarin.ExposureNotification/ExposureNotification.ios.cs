using ContactTracing;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.ExposureNotifications
{
	public static partial class ExposureNotification
	{
		static async Task PlatformStart(IExposureNotificationHandler handler)
		{
			var tcsComplete = new TaskCompletionSource<NSError>();

			var req = new CTStateSetRequest();
			req.State = true;
			req.CompletionHandler = err =>
			{
				tcsComplete.TrySetResult(err);
			};
			req.Perform();

			await tcsComplete.Task;
		}

		static async Task PlatformStop()
		{
			var tcsComplete = new TaskCompletionSource<NSError>();

			var req = new CTStateSetRequest();
			req.State = false;
			req.CompletionHandler = err =>
			{
				tcsComplete.TrySetResult(err);
			};
			req.Perform();

			await tcsComplete.Task;
		}

		static async Task<bool> PlatformIsEnabled()
		{
			var tcsComplete = new TaskCompletionSource<NSError>();

			var req = new CTStateGetRequest();
			req.CompletionHandler = err =>
			{
				tcsComplete.TrySetResult(err);
			};
			req.Perform();

			await tcsComplete.Task;

			return req.State;
		}

		// Gets the contact info of anyone the user had contact with who was diagnosed
		static async Task<IEnumerable<ExposureInfo>> PlatformGetExposureInformation()
		{
			return null;
		}

		static async Task<ExposureDetectionSummary> PlatformGetExposureSummary()
		{
			return null;
		}

		// Call this when the user has confirmed diagnosis
		static async Task PlatformSubmitPositiveDiagnosis()
		{
			
		}

		// Tells the local API when new diagnosis keys have been obtained from the server
		static async Task PlatformProcessDiagnosisKeys(IEnumerable<TemporaryExposureKey> diagnosisKeys)
		{
			
		}
	}
}
