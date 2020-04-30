using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.ExposureNotifications
{
	public interface IExposureNotificationHandler
	{
		Configuration Configuration { get; }

		// App should send keys to the backend server
		Task SubmitSelfDiagnosisKeysToServer(IEnumerable<TemporaryExposureKey> temporaryExposureKeys);

		// Go fetch the keys from your server
		Task<IEnumerable<TemporaryExposureKey>> FetchExposureKeysFromServer();

		// Might be exposed, check and alert user if necessary
		Task ExposureDetected(ExposureDetectionSummary summary, Func<Task<IEnumerable<ExposureInfo>>> getDetailsFunc);
	}
}
