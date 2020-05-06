using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.ExposureNotifications
{
	public interface IExposureNotificationHandler
	{
		Configuration Configuration { get; }

		// Go fetch the keys from your server
		IAsyncEnumerable<TemporaryExposureKey> FetchExposureKeysFromServer();

		// Might be exposed, check and alert user if necessary
		Task ExposureDetected(ExposureDetectionSummary summary, IAsyncEnumerable<ExposureInfo> details);

		Task UploadSelfExposureKeysToServer(IEnumerable<TemporaryExposureKey> temporaryExposureKeys);
	}
}
