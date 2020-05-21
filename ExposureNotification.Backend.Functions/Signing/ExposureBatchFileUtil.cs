using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using ExposureNotification.Backend.Database;
using ExposureNotification.Backend.Proto;
using ExposureNotification.Backend.Signing;
using Google.Protobuf;

namespace ExposureNotification.Backend.Functions
{
	public static class ExposureBatchFileUtil
	{
		public const string SignatureAlgorithm = "1.2.840.10045.4.3.2";
		public const string BinEntryName = "export.bin";
		public const string SigEntryName = "export.sig";

		public static async Task<Stream> CreateSignedFileAsync(TemporaryExposureKeyExport export, IEnumerable<DbSignerInfo> signerInfos, ISigner signer)
		{
			// Convert the infos into proto versions
			var signatures = signerInfos.Select(si => new SignatureInfo
			{
				AndroidPackage = si.AndroidPackage,
				AppBundleId = si.AppBundleId,
				SignatureAlgorithm = SignatureAlgorithm,
				VerificationKeyId = si.VerificationKeyId,
				VerificationKeyVersion = si.VerificationKeyVersion,
			});

			// First add all signatures to the actual export
			export.SignatureInfos.AddRange(signatures);

			var ms = new MemoryStream();

			using (var zipFile = new ZipArchive(ms, ZipArchiveMode.Create, true))
			using (var bin = await CreateBinAsync(export))
			using (var sig = await CreateSigAsync(export, bin.ToArray(), signer))
			{
				// Copy the bin contents into the entry
				var binEntry = zipFile.CreateEntry(BinEntryName, CompressionLevel.Optimal);
				using (var binStream = binEntry.Open())
				{
					await bin.CopyToAsync(binStream);
				}

				// Copy the sig contents into the entry
				var sigEntry = zipFile.CreateEntry(SigEntryName, CompressionLevel.NoCompression);
				using (var sigStream = sigEntry.Open())
				{
					await sig.CopyToAsync(sigStream);
				}
			}

			// Rewind to the front for the consumer
			ms.Position = 0;
			return ms;
		}

		public static async Task<MemoryStream> CreateBinAsync(TemporaryExposureKeyExport export)
		{
			var stream = new MemoryStream();

			// Write header
			await stream.WriteAsync(TemporaryExposureKeyExport.Header);

			// Write export proto
			export.WriteTo(stream);

			// Rewind to the front for the consumer
			stream.Position = 0;

			return stream;
		}

		public static async Task<MemoryStream> CreateSigAsync(TemporaryExposureKeyExport export, byte[] exportBytes, ISigner signer)
		{
			var stream = new MemoryStream();

			// Create signature list object
			var tk = new TEKSignatureList();
			foreach (var sigInfo in export.SignatureInfos)
			{
				// Generate the signature from the bin file contents
				var sig = await signer.GenerateSignatureAsync(exportBytes);

				tk.Signatures.Add(new TEKSignature
				{
					BatchNum = export.BatchNum,
					BatchSize = export.BatchSize,
					SignatureInfo = sigInfo,
					Signature = ByteString.CopyFrom(sig),
				});
			}

			// Write signature proto
			tk.WriteTo(stream);

			// Rewind to the front for the consumer
			stream.Position = 0;

			return stream;
		}
	}
}
