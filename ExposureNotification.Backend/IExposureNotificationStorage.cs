using ExposureNotification.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.Backend
{
	public interface IExposureNotificationStorage
	{
		Task<(DateTime timestamp, IEnumerable<TemporaryExposureKey> keys)> GetKeysAsync(DateTime? since);

		Task AddDiagnosisUidsAsync(IEnumerable<string> diagnosisUids);

		Task RemoveDiagnosisUidsAsync(IEnumerable<string> diagnosisUids);

		Task SubmitPositiveDiagnosisAsync(string diagnosisUid, IEnumerable<TemporaryExposureKey> keys);
	}
}
