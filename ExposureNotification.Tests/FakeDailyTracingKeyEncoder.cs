using ExposureNotification.Core;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExposureNotification.Tests
{
	class FakeTemporaryExposureKeyEncoder : ITemporaryExposureKeyEncoding
	{
		public byte[] Decode(byte[] encodedKeyData)
			=> encodedKeyData;

		public byte[] Encode(byte[] keyData)
			=> keyData;
	}
}
