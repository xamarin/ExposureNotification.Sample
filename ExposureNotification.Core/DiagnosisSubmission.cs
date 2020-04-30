using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.Core
{
	public class DiagnosisSubmission
	{
		[JsonProperty("diagnosisUid")]
		public string DiagnosisUid { get; set; }

		[JsonProperty("temporaryExposureKeys")]
		public List<TemporaryExposureKey> TemporaryExposureKeys { get; set; }
	}
}
