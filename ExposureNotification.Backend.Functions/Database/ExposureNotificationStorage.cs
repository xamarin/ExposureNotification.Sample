using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.ExposureNotifications;
using Xamarin.ExposureNotifications.Proto;

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

		public async Task<KeysResponse> GetKeysAsync(ulong since, int skip = 0, int take = 1000)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				var oldest = DateTimeOffset.UtcNow.AddDays(-14).ToUnixTimeMilliseconds();

				var results = await ctx.TemporaryExposureKeys.AsQueryable()
					.Where(dtk => dtk.Id > since
						&& dtk.TimestampMsSinceEpoch >= oldest)
					.OrderBy(dtk => dtk.Id)
					.Skip(skip)
					.Take(take)
					.ToListAsync().ConfigureAwait(false);

				var newestIndex = results
					.LastOrDefault()?.Id;

				var keys = results.Select(dtk => dtk.ToKey());

				return new KeysResponse
				{
					Latest = newestIndex ?? 0,
					Keys = keys
				};
			}
		}

		public async Task<File> GetKeysFileAsync(long sinceMsSinceEpoch, int batchNum, string region = "")
		{
			if (batchNum < 1)
				batchNum = 1;

			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				var query = ctx.TemporaryExposureKeys
					.Where(dtk => dtk.TimestampMsSinceEpoch >= sinceMsSinceEpoch)
					.OrderByDescending(dtk => dtk.TimestampMsSinceEpoch);

				// Get overall count for this query
				var count = await query.CountAsync();

				// Select only the batch we want
				var batch = await query
					.Skip((batchNum - 1) * Consts.MaxKeysPerFile)
					.Take(Consts.MaxKeysPerFile)
					.ToListAsync().ConfigureAwait(false);

				var f = new File
				{
					Header = new Header
					{
						BatchNum = batchNum,
						BatchSize = count,
						StartTimestamp = batch.First().TimestampMsSinceEpoch,
						EndTimestamp = batch.Last().TimestampMsSinceEpoch,
						Region = region
					}
				};

				f.Key.AddRange(batch.Select(k => new Key
				{
					KeyData = Google.Protobuf.ByteString.FromBase64(k.Base64KeyData),
					RollingPeriod = (uint)k.RollingDuration,
					RollingStartNumber = (uint)k.RollingStartSecondsSinceEpoch,
					TransmissionRiskLevel = (int)k.TransmissionRiskLevel
					// TODO: Determine if apple or google to map to appropriate
					// transmisison level number
				}).ToList());

				return f;
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
			[JsonProperty("latest")]
			public ulong Latest { get; set; }

			[JsonProperty("keys")]
			public IEnumerable<TemporaryExposureKey> Keys { get; set; }
		}
	}
}
