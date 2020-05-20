using Xunit;

namespace ExposureNotification.Backend.Functions.Tests
{
	public class ExposureBatchFileUtilTests
	{
		[Fact]
		public void CanCreateStream()
		{
			var export = Utils.GenerateTemporaryExposureKeyExport(10);

			using var stream = ExposureBatchFileUtil.CreateSignedFile(export);

			Assert.NotNull(stream);
			Assert.NotEqual(0, stream.Length);
		}
	}
}
