# ProtoBuf Files

Apple has changed their API to require submission of binary files of keys to evaluate exposure with.
The client should download these files and save them to disk, then pass them into the native API.

The spec for the protobuf is provided by apple:
https://developer.apple.com/documentation/exposurenotification/enmanager/3586331-detectexposures

Specifically the spec is:

```
/*Copyright (C) 2020 Apple Inc. All Rights Reserved.
*/

message File
{
optional Headerheader= 1;
repeated Keykey= 2;
}
​
message Header
{
optional int64 startTimestamp = 1; // Time window of keys in this file based on arrival to server, in UTC.
optional int64 endTimestamp = 2;
optional string region = 3; // Region for which these keys came from (e.g., country)
optional int32 batchNum = 4; // E.g., Batch 2 of 10
optional int32 batchSize = 5;
}
​
message Key
{
optional bytes keyData = 1; // Key of infected user
optional uint32 rollingStartNumber = 2; // Interval number when the key's EKRollingPeriod started.
optional uint32 rollingPeriod = 3; // Number of 10-minute windows between key rolling.
optional int32 transmissionRiskLevel = 4; // Risk of transmission associated with the person this key came from.
}
```

We've pre-generated the C# code for the spec and will update as necessary.

## Protobuf File Size limitations

Apple's docs suggest that 500kb is the max size for a single file you can pass to the native API.

So, we need to send the appropriate size down from the server, and to do so we need to know
just how many keys we can stick into a single file.

So let's do some math:

### Header

The header only occurs once per file, and is pretty fixed except for the 'Region' which is a string
but let's assume we are using UTF8 and we can have no more than 64 chars in the string which should
be reasonable enough:

| Column         | Data Type | Size      |                                        |
|----------------|-----------|-----------|----------------------------------------|
| StartTimestamp | long      | 8 bytes   |                                        |
| EndTimestamp   | long      | 8 bytes   |                                        |
| Region         | string    | 512 bytes | (assume 64 char's max at 8 bytes each) |
| BatchNum       | int       | 8 bytes   |                                        |
| BatchSize      | int       | 8 bytes   |                                        |
| Total          |           | 544 bytes |                                        |


### Keys

Now for keys, we can have many keys in one file, so let's calculate the fixed size of each:

| Column                | Data Type | Size     |                              |
|-----------------------|-----------|----------|------------------------------|
| KeyData               | byte[]    | 16 bytes | (according to Google's docs) |
| RollingStart          | uint      | 4 bytes  |                              |
| RollingPeriod         | uint      | 4 bytes  |                              |
| TransmissionRiskLevel | int       | 4 bytes  |                              |
| Total                 |           | 28 bytes |                              |

### Totals

So given the fixed header size and key size, at 500kb we have 512,000 bytes to work with.

Subtracting the header leaves us with 511,456 bytes.

This leaves us with **~18,266** possible keys (511,456 / 28)

Let's leave some wiggle room and say we have room for **18,000** keys per file.

