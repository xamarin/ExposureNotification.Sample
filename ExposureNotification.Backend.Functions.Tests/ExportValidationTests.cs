using System.IO;
using System.IO.Compression;
using System.Linq;
using ExposureNotification.Backend.Proto;
using Xunit;

namespace ExposureNotification.Backend.Functions.Tests
{
	public class ExportValidationTests
	{
		[Fact]
		public void ValidateSignedExample()
		{
			using var zipFile = ZipFile.OpenRead("TestAssets/SignedExample/export.zip");

			var expectedEntries = new[] { "export.bin", "export.sig" };
			var entries = zipFile.Entries.Select(e => e.FullName);
			Assert.Equal(expectedEntries, entries);
		}

		[Fact]
		public void ValidateSignedExampleBinary()
		{
			using var zipFile = ZipFile.OpenRead("TestAssets/SignedExample/export.zip");
			using var exportBin = zipFile.GetBin();

			var export = TemporaryExposureKeyExport.Parser.ParseFrom(exportBin);
			Assert.NotNull(export);

			var info = Assert.Single(export.SignatureInfos);
			Assert.Equal("1.2.840.10045.4.3.2", info.SignatureAlgorithm);
			Assert.Equal("ExampleServer_k1", info.VerificationKeyId);
		}

		[Fact]
		public void ValidateSignedExampleSignature()
		{
			using var zipFile = ZipFile.OpenRead("TestAssets/SignedExample/export.zip");
			using var exportSig = zipFile.GetSignature();

			var signatureList = TEKSignatureList.Parser.ParseFrom(exportSig);
			Assert.NotNull(signatureList);

			var signature = Assert.Single(signatureList.Signatures);
			Assert.NotEmpty(signature.Signature.ToByteArray());

			var info = signature.SignatureInfo;
			Assert.Equal("1.2.840.10045.4.3.2", info.SignatureAlgorithm);
			Assert.Equal("ExampleServer_k1", info.VerificationKeyId);
		}

		[Fact]
		public void ValidateSignedExampleSignatureValidity()
		{
			using var zipFile = ZipFile.OpenRead("TestAssets/SignedExample/export.zip");
			using var exportSig = zipFile.GetSignature();
			using var exportBin = zipFile.GetBin(false);

			var signatureList = TEKSignatureList.Parser.ParseFrom(exportSig);
			var signature = signatureList.Signatures[0].Signature.ToByteArray();

			var pem = File.ReadAllText("TestAssets/SignedExample/public.pem");

			var bin = exportBin.ToArray();

			Assert.True(Utils.ValidateSignature(bin, signature, pem));
		}

		[Fact]
		public void ValidateKeysExample()
		{
			using var zipFile = ZipFile.OpenRead("TestAssets/KeysExample/export.zip");

			var expectedEntries = new[] { "export.bin", "export.sig" };
			var entries = zipFile.Entries.Select(e => e.FullName);
			Assert.Equal(expectedEntries, entries);

			using var exportBin = zipFile.GetBin();

			var export = TemporaryExposureKeyExport.Parser.ParseFrom(exportBin);
			Assert.NotNull(export);
			Assert.Equal(110, export.Keys.Count);

			var info = Assert.Single(export.SignatureInfos);
			Assert.Equal("com.google.android.apps.exposurenotification", info.AndroidPackage);
		}
	}
}
