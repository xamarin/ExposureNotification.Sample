using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.App.Services
{
	public static class LocalStateManager
	{
		static LocalState instance;

		public static LocalState Instance
			=> instance ??= Load() ?? new LocalState();

		static LocalState Load()
		{
			try
			{
				var stateFile = Path.Combine(Xamarin.Essentials.FileSystem.CacheDirectory, "localstate.json");

				var data = File.ReadAllText(stateFile);

				return Newtonsoft.Json.JsonConvert.DeserializeObject<LocalState>(data);
			}
			catch
			{
				return new LocalState();
			}
		}

		public static void Save()
		{
			var stateFile = Path.Combine(Xamarin.Essentials.FileSystem.CacheDirectory, "localstate.json");

			var data = Newtonsoft.Json.JsonConvert.SerializeObject(Instance);

			File.WriteAllText(stateFile, data);
		}
	}

	public class LocalState
	{
		public bool IsWelcomed { get; set; }

		public bool LastIsEnabled { get; set; } = false;

		public bool EnableNotifications { get; set; } = true;

		public ulong ServerBatchNumber { get; set; } = 1;

		public string Region { get; set; } = ExposureNotificationHandler.DefaultRegion;

		public List<ExposureInfo> ExposureInformation { get; set; } = new List<ExposureInfo>();

		public ExposureDetectionSummary ExposureSummary { get; set; }

		public List<PositiveDiagnosisState> PositiveDiagnoses { get; set; } = new List<PositiveDiagnosisState>();

		public void AddDiagnosis(string diagnosisUid, DateTimeOffset submissionDate)
		{
			var existing = PositiveDiagnoses?.Where(d => d.DiagnosisUid.Equals(diagnosisUid, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(d => d.DiagnosisDate).FirstOrDefault();

			if (existing != null)
				return;

			PositiveDiagnoses.Add(new PositiveDiagnosisState
			{
				DiagnosisDate = submissionDate,
				DiagnosisUid = diagnosisUid,
			});
		}

		public PositiveDiagnosisState LatestDiagnosis
			=> PositiveDiagnoses?
				.Where(d => d.Shared)
				.OrderByDescending(p => p.DiagnosisDate)?
				.FirstOrDefault();

		public PositiveDiagnosisState PendingDiagnosis
			=> PositiveDiagnoses?
				.Where(d => !d.Shared)
				.OrderByDescending(p => p.DiagnosisDate)?
				.FirstOrDefault();
	}

	public class PositiveDiagnosisState
	{
		public string DiagnosisUid { get; set; }

		public DateTimeOffset DiagnosisDate { get; set; }

		public bool Shared { get; set; }
	}
}
