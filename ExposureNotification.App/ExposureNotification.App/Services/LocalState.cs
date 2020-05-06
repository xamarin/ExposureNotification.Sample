using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.App.Services
{
	public class LocalState
	{
		public List<ExposureInfo> ExposureInformation { get; set; } = new List<ExposureInfo>();
		public ExposureDetectionSummary ExposureSummary { get; set; }
		public List<PositiveDiagnosisState> PositiveDiagnoses { get; set; } = new List<PositiveDiagnosisState>();
	}

	public class PositiveDiagnosisState
	{
		public string DiagnosisUid { get; set; }
		public DateTime DiagnosisDate { get; set; }
	}
}
