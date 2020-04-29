using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExposureNotification.Backend
{
	public interface IExposureNotificationStorage
	{
		Task<IEnumerable<TemporaryExposureKey>> GetKeysAsync(DateTime? since);

		Task AddDiagnosisUidsAsync(IEnumerable<string> diagnosisUids);

		Task RemoveDiagnosisUidsAsync(IEnumerable<string> diagnosisUids);

		Task SubmitPositiveDiagnosisAsync(string diagnosisUid, IEnumerable<TemporaryExposureKey> keys);
	}
}
