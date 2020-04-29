using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExposureNotification.Backend
{
	class DbTemporaryExposureKey
	{
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();

		public string Base64KeyData { get; set; }

		public DateTime Timestamp { get; set; }
	}
}
