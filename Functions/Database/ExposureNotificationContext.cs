using Microsoft.EntityFrameworkCore;

namespace Functions.Database
{
	public class ExposureNotificationContext : DbContext
	{
		public ExposureNotificationContext()
		{
		}

		public ExposureNotificationContext(DbContextOptions options)
			: base(options)
		{
		}

		public DbSet<DbTemporaryExposureKey> TemporaryExposureKeys { get; set; }

		public DbSet<DbDiagnosis> Diagnoses { get; set; }
	}
}
