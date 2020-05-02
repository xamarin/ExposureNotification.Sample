using System.ComponentModel.DataAnnotations;

namespace ExposureNotification.Backend
{
	class DbDiagnosis
	{
		public DbDiagnosis(string diagnosisUid)
			=> DiagnosisUid = diagnosisUid;

		[Key]
		public string DiagnosisUid { get; set; }
	}
}
