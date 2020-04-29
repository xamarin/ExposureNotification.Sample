using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace ExposureNotification.Backend
{
	public class ExposureNotificationStorage : IExposureNotificationStorage
	{
		public ExposureNotificationStorage(ITemporaryExposureKeyEncoding tempExposureKeyEncoding,
			Action<DbContextOptionsBuilder> buildDbContextOpetions = null,
			Action<DbContext> initializeDb = null)
		{
			var dbContextOptionsBuilder = new DbContextOptionsBuilder();
			buildDbContextOpetions?.Invoke(dbContextOptionsBuilder);
			dbContextOptions = dbContextOptionsBuilder.Options;

			temporaryExposureKeyConding = tempExposureKeyEncoding;

			using (var ctx = new ExposureNotificationContext(dbContextOptions))
				initializeDb?.Invoke(ctx);
		}

		readonly DbContextOptions dbContextOptions;
		readonly ITemporaryExposureKeyEncoding temporaryExposureKeyConding;

		public async Task<IEnumerable<TemporaryExposureKey>> GetKeysAsync(DateTime? since)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				var oldest = DateTime.UtcNow.AddDays(-14);

				if (!since.HasValue || since.Value.ToUniversalTime() < oldest)
					since = oldest;

				var results = await ctx.TemporaryExposureKeys.AsQueryable()
					.Where(dtk => dtk.Timestamp >= since)
					.ToListAsync().ConfigureAwait(false);

				return results.Select(dtk => new TemporaryExposureKey {
					KeyData = temporaryExposureKeyConding.Decode(Convert.FromBase64String(dtk.Base64KeyData)),
					Timestamp = dtk.Timestamp
				});
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

				var dbKeys = keys.Where(k => k.Timestamp.ToUniversalTime() >= DateTime.UtcNow.AddDays(-14))
					.Select(k => new DbTemporaryExposureKey
						{
							Base64KeyData = Convert.ToBase64String(temporaryExposureKeyConding.Encode(k.KeyData)),
							Timestamp = k.Timestamp
						}).ToList();

				foreach (var dbk in dbKeys)
					ctx.TemporaryExposureKeys.Add(dbk);

				await ctx.SaveChangesAsync();
			}
		}
	}
}
