
# Xamarin Exposure Notification

[Read our Planning Document with more details about how Exposure Notifications work](https://github.com/xamarin/xamarin.exposurenotification/blob/master/Exposure%20Notification%20Planning.pdf)

Apple and Google are both creating API’s for a compatible BLE based Contact Tracing implementation which relies heavily on generating and storing rolling unique identifiers on a device, which are broadcast to nearby devices.  Devices which detect nearby identifiers then store these identifiers as they come into range (or contact) with for up to 14 days.

When a person has confirmed a diagnosis, they tell their device which then submits the locally stored, self-generated rolling unique identifiers from the last 14 days to a back end service provided by the app implementing the API.

Devices continually request the keys submitted by diagnosed people from the backend server.  The device then compares these keys to the unique identifiers of other devices it has been near in the last 14 days.

## Xamarin.ExposureNotification

This project contains the cross platform wrapper API around the native Android and iOS API's.  The sample app uses this library to implement the exposure notification code one time for both platforms.

## Sample App

A sample Mobile app and Back end server to use the API.

## Bindings to Native API's - NuGet

We also have NuGet packages available with bindings to the native Android and iOS Exposure Notifaction API's

 - iOS https://www.nuget.org/packages/Xamarin.iOS.ExposureNotification/ (Requires XCode 11.5 beta1 or newer)
 - Android https://www.nuget.org/packages/Xamarin.GooglePlayServices.Nearby.ExposureNotification/

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
