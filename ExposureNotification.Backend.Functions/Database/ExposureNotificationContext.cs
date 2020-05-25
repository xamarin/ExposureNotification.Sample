using Microsoft.EntityFrameworkCore;

namespace ExposureNotification.Backend
{
	class ExposureNotificationContext : DbContext
	{
		public ExposureNotificationContext(DbContextOptions options)
			: base(options)
		{
		}

		public DbSet<DbTemporaryExposureKey> TemporaryExposureKeys { get; set; }

		public DbSet<DbDiagnosis> Diagnoses { get; set; }
	}
}
