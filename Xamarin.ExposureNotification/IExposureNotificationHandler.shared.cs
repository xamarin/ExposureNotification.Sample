using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.ExposureNotifications
{
	public interface IExposureNotificationHandler
	{
		string UserExplanation { get; }

		Task<Configuration> GetConfigurationAsync();

		// Go fetch the keys from your server
		Task FetchExposureKeysFromServerAsync(ITemporaryExposureKeyBatches batches);

		// Might be exposed, check and alert user if necessary
		Task ExposureDetectedAsync(ExposureDetectionSummary summary, IEnumerable<ExposureInfo> ExposureInfo);

		Task UploadSelfExposureKeysToServerAsync(IEnumerable<TemporaryExposureKey> temporaryExposureKeys);
	}

	public interface ITemporaryExposureKeyBatches
	{
		Task AddBatchAsync(IEnumerable<TemporaryExposureKey> keys);

		Task AddBatchAsync(Proto.File file);
	}

	public interface IMockExposureNotificationApi
	{
		Task StartAsync();
		Task StopAsync();
		Task<bool> IsEnabledAsync();
		Task<(ExposureDetectionSummary summary, IEnumerable<ExposureInfo> info)> DetectExposuresAsync(TemporaryExposureKeyBatches batches);
		Task<IEnumerable<TemporaryExposureKey>> GetSelfTemporaryExposureKeysAsync();
	}
}
