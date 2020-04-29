using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Sql;

namespace Android.Gms.ContactTracing
{
	public enum Status
	{
		Success = 0,
		FailedRejectedOptIn = 1,
		FailedServiceDisabled = 2,
		FailedBluetoothScanningDisabled = 3,
		FailedTemporarilyDisabled = 4,
		FailedInsufficientStorage = 5,
		FailedInternal = 6,
	}

	public class ContactTracing
	{
		/**
		* Starts BLE broadcasts and scanning based on the defined protocol.
		*
		* If not previously used, this shows a user dialog for consent to start contact
		* tracing and get permission.
		*
		* Calls back when data is to be pushed or pulled from the client, see
		* ContactTracingCallback.
		*
		* Callers need to re-invoke this after each device restart, providing a new
		* callback PendingIntent.
		*/
		public Task<Status> StartContactTracing(PendingIntent contactTracingCallback)
		{


			return Task.FromResult(Status.Success);
		}

		/**
		* Disables advertising and scanning related to contact tracing. Contents of the
		* database and keys will remain.
		*
		* If the client app has been uninstalled by the user, this will be automatically
		* invoked and the database and keys will be wiped from the device.
		*/
		public Task<Status> StopContactTracing()
		{

			return Task.FromResult(Status.Success);
		}

		/**
		* Indicates whether contact tracing is currently running for the
		* requesting app.
		*/
		public Task<Status> IsContactTracingEnabled()
		{

			return Task.FromResult(Status.Success);
		}

		/**
		* Flags daily tracing keys as to be stored on the server.
		*
		* This should only be done after proper verification is performed on the
		* client side that the user is diagnosed positive.
		*
		* Calling this will invoke the
		* ContactTracingCallback.requestUploadDailyTracingKeys callback
		* provided via startContactTracing at some point in the future. Provided keys
		* should be uploaded to the server and distributed to other users.
		*
		* This shows a user dialog for sharing and uploading data to the server.
		* The status will also flip back off again after 14 days; in other words,
		* the client will stop receiving requestUploadDailyTracingKeys
		* callbacks after that time.
		*
		* Only 14 days of history are available.
		*/
		public Task<Status> StartSharingDailyTracingKeys()
		{

			return Task.FromResult(Status.Success);
		}

		/**
		* Provides a list of diagnosis keys for contact checking. The keys are to be
		* provided by a centralized service (e.g. synced from the server).
		*
		* When invoked after the requestProvideDiagnosisKeys callback, this triggers a
		* recalculation of contact status which can be obtained via hasContact()
		* after the calculation has finished.
		*
		* Should be called with a maximum of N keys at a time.
		*/
		public Task<Status> ProvideDiagnosisKeys(List<DailyTracingKey> keys)
		{

			return Task.FromResult(Status.Success);
		}

		/**
		* The maximum number of keys to pass into provideDiagnosisKeys at any given
		* time.
		*/
		public int MaxDiagnosisKeys
			=> 10;


		/**
* Check if this user has come into contact with a provided key. Contact
* calculation happens daily.
*/
		public Task<bool> HasContact()
		{

			return Task.FromResult(true);
		}

		/**
		* Check if this user has come into contact with a provided key. Contact
		* calculation happens daily.
*/
		public Task<List<IContactInfo>> GetContactInformation()
		{

			var l = new List<IContactInfo>
			{
				new ContactInfo(DateTime.UtcNow.AddDays(-7), 10)
			};

			return Task.FromResult(l);
		}

	}


	class ContactInfo : IContactInfo
	{
		public ContactInfo(DateTime date, int duration)
		{
			ContactDate = date;
			Duration = duration;
		}

		public DateTime ContactDate { get; }
		public int Duration { get; }
	}
	/**
	* Handles an intent which was invoked via the contactTracingCallback and
	* calls the corresponding ContactTracingCallback methods.
*/
	//void handleIntent(Intent intentCallback, ContactTracingCallback callback);



	public interface IContactInfo
	{
		/** Day-level resolution that the contact occurred. */
		public DateTime ContactDate { get; }
		/** Length of contact in 5 minute increments. */
		public int Duration { get; }
	}


	interface IContactTracingCallback
	{
		// Notifies the client that the user has been exposed and they should
		// be warned by the app of possible exposure.
		void OnContact();
		// Requests client to upload the provided daily tracing keys to their server for
		// distribution after the other user’s client receives the
		// requestProvideDiagnosisKeys callback. The keys provided here will be at
		// least 24 hours old.
		//
		// In order to be whitelisted to use this API, apps will be required to timestamp
		// and cryptographically sign the set of keys before delivery to the server
		// with the signature of an authorized medical authority.
		void RequestUploadDailyTracingKeys(List<DailyTracingKey> keys);
		// Requests client to provide a list of all diagnosis keys from the server.
		// This should be done by invoking provideDiagnosisKeys().
		void RequestProvideDiagnosisKeys();
	}

	public class DailyTracingKey
	{
		public DailyTracingKey(byte[] key, DateTime date)
		{
			Key = key;
			Date = date;
		}

		public byte[] Key { get; }
		public DateTime Date { get; } // Day-level granularity.
	}
}
