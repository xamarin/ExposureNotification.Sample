using System.ComponentModel.DataAnnotations;

namespace ExposureNotification.Backend.Database
{
	class DbDiagnosis
	{
		public DbDiagnosis(string diagnosisUid)
			=> DiagnosisUid = diagnosisUid;

		[Key]
		public string DiagnosisUid { get; set; }

		public int KeyCount { get; set; }
	}
}
