using System.Diagnostics;
using System.Runtime.CompilerServices;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;
using Org.BouncyCastle.OpenSsl;

namespace ExposureNotification.Backend.Functions.Tests
{
	public class ExportValidationTests
	{
		[Fact]
		public void ValidateSignedExample()
		{
			var exportFile = "TestAssets/SignedExample/export.zip";

			using var zipFile = ZipFile.OpenRead(exportFile);

			var expectedEntries = new[] { "export.bin", "export.sig" };
			var entries = zipFile.Entries.Select(e => e.FullName);
			Assert.Equal(expectedEntries, entries);
		}

		[Fact]
		public void ValidateSignedExampleBinary()
		{
			var exportFile = "TestAssets/SignedExample/export.zip";

			using var zipFile = ZipFile.OpenRead(exportFile);
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
			var exportFile = "TestAssets/SignedExample/export.zip";

			using var zipFile = ZipFile.OpenRead(exportFile);
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
			var exportFile = "TestAssets/SignedExample/export.zip";
			using var zipFile = ZipFile.OpenRead(exportFile);
			using var exportSig = zipFile.GetSignature();
			using var exportBin = zipFile.GetBin();

			var signatureList = TEKSignatureList.Parser.ParseFrom(exportSig);
			var signature = signatureList.Signatures[0].Signature.ToByteArray();

			var pemFile = "TestAssets/SignedExample/public.pem";
			var pem = File.ReadAllText(pemFile);

			var bin = exportBin.ToArray();

			Assert.True(Validate(signature, pem, bin));
		}

		public bool Validate(byte[] signature, string pem, byte[] data)
		{
			using var stringReader = new StringReader(pem);
			var reader = new PemReader(stringReader);
			var obj = reader.ReadObject();

			return false;
		}

		[Fact]
		public void ValidateKeysExample()
		{
			var exportFile = "TestAssets/KeysExample/export.zip";

			using var zipFile = ZipFile.OpenRead(exportFile);

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
