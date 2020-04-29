using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Foundation;

namespace ContactTracing
{
	public class CTStateGetRequest
	{
		public Action<NSError> CompletionHandler { get; set; }

		public bool State { get; }

		public void Perform()
		{ }

		public void Invalidate()
		{ }
	}

	public class CTStateSetRequest
	{
		public Action<NSError> CompletionHandler { get; set; }
		public bool State { get; set; }

		public void Perform()
		{ }

		public void Invalidate()
		{ }
	}

	public class CTExposureDetectionSession
	{
		public int MaxKeyCount { get; }

		public event EventHandler<CTExposureDetectionSummary> FinishedPositiveDiagnosisKeys;

		public Task<NSError> ActivateAsync()
			=> Task.FromResult<NSError>(default);

		public void Invalidate()
		{ }

		public Task<NSError> AddPositiveDiagnosisKeysAsync(List<CTDailyTracingKey> inKeys)
			=> Task.FromResult<NSError>(default);

		public Task<List<CTContactInfo>> GetContactInfoAsync()
			=> Task.FromResult(new List<CTContactInfo>());
	}

	public class CTExposureDetectionSummary
	{
		public int MatchedKeyCount { get; }
	}

	public class CTSelfTracingInfoRequest
	{
		public Action<CTSelfTracingInfo, NSError> CompletionHandler { get; set; }

		public void Perform()
		{ }

		public void Invalidate()
		{ }
	}

	public class CTSelfTracingInfo
	{
		public CTDailyTracingKey[] DailyTracingKeys { get; }
	}

	public class CTContactInfo
	{
		public TimeSpan Duraction { get; }
		public DateTime Timestamp { get; }
	}

	public class CTDailyTracingKey
	{
		public NSData KeyData { get; }
	}
}
