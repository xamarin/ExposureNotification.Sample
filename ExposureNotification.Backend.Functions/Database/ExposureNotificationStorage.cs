using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.Backend
{
	public class ExposureNotificationStorage
	{
		public ExposureNotificationStorage(
			Action<DbContextOptionsBuilder> buildDbContextOpetions = null,
			Action<DbContext> initializeDb = null)
		{
			var dbContextOptionsBuilder = new DbContextOptionsBuilder();
			buildDbContextOpetions?.Invoke(dbContextOptionsBuilder);
			dbContextOptions = dbContextOptionsBuilder.Options;

			using (var ctx = new ExposureNotificationContext(dbContextOptions))
				initializeDb?.Invoke(ctx);
		}

		readonly DbContextOptions dbContextOptions;

		public async Task<(DateTime, IEnumerable<TemporaryExposureKey>)> GetKeysAsync(DateTime? since)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				var oldest = DateTime.UtcNow.AddDays(-14);

				if (!since.HasValue || since.Value.ToUniversalTime() < oldest)
					since = oldest;

				var results = await ctx.TemporaryExposureKeys.AsQueryable()
					.Where(dtk => dtk.Timestamp >= since)
					.ToListAsync().ConfigureAwait(false);

				var newestTimestamp = results.OrderByDescending(dtk => dtk.Timestamp).FirstOrDefault()?.Timestamp;
				var keys = results.Select(dtk => dtk.ToKey());

				return (newestTimestamp ?? DateTime.MinValue, keys);
			}
		}

		public async Task AddDiagnosisUidsAsync(IEnumerable<string> diagnosisUids)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				foreach (var d in diagnosisUids)
				{
					if (!(await ctx.Diagnoses.AnyAsync(r => r.DiagnosisUid == d)))
						ctx.Diagnoses.Add(new DbDiagnosis(d));
				}

				await ctx.SaveChangesAsync();
			}
		}

		public async Task RemoveDiagnosisUidsAsync(IEnumerable<string> diagnosisUids)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				var toRemove = new List<DbDiagnosis>();

				foreach (var d in diagnosisUids)
				{
					var existingUid = await ctx.Diagnoses.FindAsync(d);
					if (existingUid != null)
						toRemove.Add(existingUid);
				}

				ctx.Diagnoses.RemoveRange(toRemove);
				await ctx.SaveChangesAsync();
			}
		}

		public Task<bool> CheckIfDiagnosisUidExistsAsync(string diagnosisUid)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
				return Task.FromResult(ctx.Diagnoses.Any(d => d.DiagnosisUid.Equals(diagnosisUid)));
		}

		public async Task SubmitPositiveDiagnosisAsync(string diagnosisUid, IEnumerable<TemporaryExposureKey> keys)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				// Ensure the database contains the diagnosis uid
				if (!ctx.Diagnoses.Any(d => d.DiagnosisUid == diagnosisUid))
					throw new InvalidOperationException();

				var dbKeys = keys.Select(k => DbTemporaryExposureKey.FromKey(k)).ToList();

				foreach (var dbk in dbKeys)
					ctx.TemporaryExposureKeys.Add(dbk);

				await ctx.SaveChangesAsync();
			}
		}
	}
}
