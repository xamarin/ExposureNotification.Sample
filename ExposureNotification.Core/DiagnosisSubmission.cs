using System;
using System.Collections.Generic;
using System.Text;

namespace ExposureNotification.App
{
	public class DiagnosisSubmission
	{
		public string DiagnosisUid { get; set; }

		public List<TemporaryExposureKey> TemporaryExposureKeys { get; set; }
	}
}
