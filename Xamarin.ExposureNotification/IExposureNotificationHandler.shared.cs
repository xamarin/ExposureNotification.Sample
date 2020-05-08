using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.ExposureNotifications
{
	public interface IExposureNotificationHandler
	{
		string UserExplanation { get; }

		Task<Configuration> GetConfigurationAsync();

		// Go fetch the keys from your server
		Task<IEnumerable<TemporaryExposureKey>> FetchExposureKeysFromServerAsync();

		// Might be exposed, check and alert user if necessary
		Task ExposureDetectedAsync(ExposureDetectionSummary summary, IEnumerable<ExposureInfo> ExposureInfo);

		Task UploadSelfExposureKeysToServerAsync(IEnumerable<TemporaryExposureKey> temporaryExposureKeys);
	}
}
