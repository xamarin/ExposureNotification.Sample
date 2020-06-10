using System.ComponentModel.DataAnnotations;

namespace Functions.Database
{
	public class DbDiagnosis
	{
		public DbDiagnosis(string diagnosisUid)
			=> DiagnosisUid = diagnosisUid;

		[Key]
		public string DiagnosisUid { get; set; }

		public int KeyCount { get; set; }
	}
}
