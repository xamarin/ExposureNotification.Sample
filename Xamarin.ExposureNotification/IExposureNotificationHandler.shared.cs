﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xamarin.ExposureNotifications
{
	public interface IExposureNotificationHandler
	{
		string UserExplanation { get; }

		Task<Configuration> GetConfigurationAsync();

		// Go fetch the keys from your server
		Task FetchExposureKeyBatchFilesFromServerAsync(Func<IEnumerable<string>, Task> submitBatches);

		// Might be exposed, check and alert user if necessary
		Task ExposureDetectedAsync(ExposureDetectionSummary summary, IEnumerable<ExposureInfo> ExposureInfo);

		Task UploadSelfExposureKeysToServerAsync(IEnumerable<TemporaryExposureKey> temporaryExposureKeys);
	}

	public interface INativeImplementation
	{
		Task StartAsync();

		Task StopAsync();

		Task<bool> IsEnabledAsync();

		Task<(ExposureDetectionSummary summary, IEnumerable<ExposureInfo> info)> DetectExposuresAsync(IEnumerable<string> files);

		Task<IEnumerable<TemporaryExposureKey>> GetSelfTemporaryExposureKeysAsync();
	}
}
