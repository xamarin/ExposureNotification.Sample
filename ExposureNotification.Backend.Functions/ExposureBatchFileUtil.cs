using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text;
using Google.Protobuf;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.Backend.Functions
{
	class ExposureBatchFileUtil
	{
		// Header is 16 UTF8 characters with right padding
		const string exportBinHeader = "EK Export v1    ";

		// TODO: You need to sign this with the key and mechanism
		// that google/apple shared with you
		static byte[] SignSignature(byte[] contents)
			=> contents;

		static SignatureInfo[] Signatures
			=> new SignatureInfo[]
			{
				new SignatureInfo
				{
					AndroidPackage = "com.xamarin.exposurenotificationsample",
					AppBundleId = "com.xamarin.exposurenotificationsample",
					VerificationKeyId = "1",
					VerificationKeyVersion = "1",
					SignatureAlgorithm = ""
				}
			};

		public static Stream CreateSignedFile(TemporaryExposureKeyExport export)
		{
			export.SignatureInfos.AddRange(Signatures);

			var ms = new MemoryStream();
			using (var zipFile = new ZipArchive(ms, ZipArchiveMode.Create, true))
			{
				var binEntry = zipFile.CreateEntry("export.bin", CompressionLevel.Optimal);

				using (var binStream = binEntry.Open())
				{
					// Write header
					binStream.Write(Encoding.UTF8.GetBytes(exportBinHeader));

					// Write export proto
					export.WriteTo(binStream);
				}

				var sigEntry = zipFile.CreateEntry("export.sig", CompressionLevel.NoCompression);

				using (var sigStream = sigEntry.Open())
				{
					var tk = new TEKSignatureList();

					foreach (var sigInfo in export.SignatureInfos)
					{
						tk.Signatures.Add(new TEKSignature
						{
							BatchNum = export.BatchNum,
							BatchSize = export.BatchSize,
							SignatureInfo = sigInfo,
							// TODO: Signature in X9.62 format (ASN.1 SEQUENCE of two INTEGER fields)
							Signature = ByteString.CopyFrom(0, 0)
						});
					}

					// Proto to byte array that needs signing
					var signature = tk.ToByteArray();

					// Call the signing function
					var signedSignature = SignSignature(signature);

					// Write out the zip entry with signed data
					sigStream.Write(signedSignature);
				}
			}

			// Rewind to the front for the consumer
			ms.Seek(0, SeekOrigin.Begin);

			return ms;
		}
	}
}
