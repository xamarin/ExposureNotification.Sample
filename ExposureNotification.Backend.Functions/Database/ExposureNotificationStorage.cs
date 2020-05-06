using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
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

		public async Task<KeysResponse> GetKeysAsync(long sinceEpochSeconds, int skip = 0, int take = int.MaxValue)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				var oldest = DateTimeOffset.UtcNow.AddDays(-14).ToUnixTimeSeconds();

				// Only allow the last 14 days +
				if (sinceEpochSeconds < oldest)
					sinceEpochSeconds = oldest;

				var results = await ctx.TemporaryExposureKeys.AsQueryable()
					.Where(dtk => dtk.TimestampSecondsSinceEpoch >= sinceEpochSeconds)
					.OrderByDescending(dtk => dtk.TimestampSecondsSinceEpoch)
					.Skip(skip)
					.Take(take)
					.ToListAsync().ConfigureAwait(false);

				var newestTimestamp = results
					.FirstOrDefault()?.TimestampSecondsSinceEpoch;

				var keys = results.Select(dtk => dtk.ToKey());

				return new KeysResponse
				{
					Timestamp = newestTimestamp ?? DateTimeOffset.MinValue.ToUnixTimeSeconds(),
					Keys = keys
				};
			}
		}

		public void DeleteAllKeysAsync()
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				ctx.TemporaryExposureKeys.RemoveRange(ctx.TemporaryExposureKeys);
				ctx.SaveChanges();
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

		public async Task SubmitPositiveDiagnosisAsync(SelfDiagnosisSubmissionRequest diagnosis)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				// Ensure the database contains the diagnosis uid
				if (!ctx.Diagnoses.Any(d => d.DiagnosisUid == diagnosis.DiagnosisUid))
					throw new InvalidOperationException();

				var dbKeys = diagnosis.Keys.Select(k => DbTemporaryExposureKey.FromKey(k)).ToList();

				foreach (var dbk in dbKeys)
					ctx.TemporaryExposureKeys.Add(dbk);

				await ctx.SaveChangesAsync();
			}
		}

		public class SelfDiagnosisSubmissionRequest
		{
			[JsonProperty("diagnosisUid")]
			public string DiagnosisUid { get; set; }

			[JsonProperty("keys")]
			public IEnumerable<TemporaryExposureKey> Keys { get; set; }
		}

		public class KeysResponse
		{
			[JsonProperty("timestamp")]
			public long Timestamp { get; set; }

			[JsonProperty("keys")]
			public IEnumerable<TemporaryExposureKey> Keys { get; set; }
		}
	}
}
