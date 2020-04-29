using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExposureNotification.App
{
	public class DiagnosisSubmission
	{
		[JsonProperty("diagnosisUid")]
		public string DiagnosisUid { get; set; }

		[JsonProperty("temporaryExposureKeys")]
		public List<TemporaryExposureKey> TemporaryExposureKeys { get; set; }
	}
}
