using ExposureNotification.Backend.Proto;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ExposureNotification.Backend.Network
{
	public class SelfDiagnosisSubmissionRequest
	{
		[JsonProperty("diagnosisUid")]
		public string DiagnosisUid { get; set; }

		[JsonProperty("testDate")]
		public long TestDate { get; set; }

		[JsonProperty("keys")]
		public IEnumerable<TemporaryExposureKey> Keys { get; set; }
	}
}
