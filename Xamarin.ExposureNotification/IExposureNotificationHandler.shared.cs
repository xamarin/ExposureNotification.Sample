using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.ExposureNotifications
{
	public interface IExposureNotificationHandler
	{
		Configuration Configuration { get; }

		// Go fetch the keys from your server
		Task FetchExposureKeysFromServer(Func<IEnumerable<TemporaryExposureKey>, Task> addKeys);

		// Might be exposed, check and alert user if necessary
		Task ExposureDetected(ExposureDetectionSummary summary, Func<Task<IEnumerable<ExposureInfo>>> getDetailsFunc);

		Task UploadSelfExposureKeysToServer(IEnumerable<TemporaryExposureKey> temporaryExposureKeys);
	}
}
