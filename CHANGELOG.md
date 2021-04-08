# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
 - Add IByodHubDevice & IByodHubDeviceSettings
 - Added property to Cisco Codec VideoComponent to determine if the codec supports video mute based on system software version.
 - Added cameras collection to IConferenceManager
 - Added features for room-wide camera privacy mute

## [17.2.2] - 2021-04-08
### Added
 - Cisco - added additional video connector signal states of "Unstable" "NotFound" and "Unsupported"

## [17.2.1] - 2021-02-18
### Changed
 - Fixed Cisco conference control supported features to be accurate
 - Implemented camera mute correctly in Cisco conference control

## [17.2.0] - 2021-01-14
### Added
 - Vaddio AV Bridge device driver
 - Added a ContentInput property for VideoConferenceRoutControls
 - Added three new flags to the eConferenceFeatures enum: CameraEnabled, Hold, & Dtmf. Implemented in conference controls where applicable.
 - Added a ConferenceManager extension method to return only the common set of supported conference features between all active dialers.
 - Added Supported Calendar Features enum property to Cisco/Polycom/Zoom Calendar Controls

### Changed
 - The CameraEnabled property has been moved from AbstractWebConferenceDeviceControl to AbstractConferenceControl
 - Moved Cisco Codec telemetry from external telemetry provider to telemetry component, using Monitored Device Telemetry
 - Moved Cisco Camera telemetry to Monitored Device Telemetry
 - CiscoCodecVolumeControl now reports the pre-mute volume level after being muted
 - CiscoCodecCameraDevice correctly reflects the power state of the connected codec device

## [17.1.2] - 2020-09-24
### Changed
 - Fixed a bug where default conference activities were not being initialized

## [17.1.1] - 2020-08-13
### Changed
 - Telemetry namespace change

## [17.1.0] - 2020-07-14
### Added
 - Added DialContextEqualityComparer

### Changed
 - Fixed a bug where ORM members were being obfuscated
 - Zoom and Polycom return bookings for the full day
 - Simplified external telemetry providers
 - Favorites are now managed through Favorite static methods

## [17.0.0] - 2020-06-18
### Added
 - Added features for getting Zoom meeting IDs in human readable format
 - Added routing control to mock conference device
 - Conference controls provide a CallInInfo property for a call-in DialContext
 - Added support for personalized Zoom "my" URLs
 - Added features for selecting Zoom audio input/output devices
 - Added configuration items for specifying Zoom audio input/output devices
 - Zoom device will force audio input/output devices back to the previously selected device
 
### Changed
 - MockConferecingDevice now inherits from AbstractMockDevice
 - Using new logging context
 - Favorites are now stored per room ORM databases
 - Fixed a bug where passwords were not being saved to contact favorites
 
### Removed
 - Removed IFavorites and SQLiteFavorites implementation
 - Removed redundant DialContext implementations

## [16.1.2] - 2021-01-07
### Added
 - ConferencePointSettings exposes const for factory name

## [16.1.1] - 2020-11-16
### Changed
 - Fixed ConferenceManager.GetBestDialer() to respect call types on conference points

## [16.1.0] - 2020-10-06
### Changed
 - Implemented StartSettings to start communnications with devices

## [16.0.2] - 2020-08-21
### Changed
 - Refacored a small portion of ConferenceManagerHistory, making sure no events are raised within a critical section to prevent deadlocks.

## [16.0.1] - 2020-08-03
### Changed
 - Correctly parse the CallType for Cisco Bookings
 - Can no longer use the Cisco to dial contexts with CallType unknown

## [16.0.0] - 2020-05-23
### Added
 - Added ConferenceHistory to the ConfernceManager, replacing recent calls
 - Added OnStartTimeChanged, OnEndTimeChanged to IConference and IParticipant
 - Added eCallAnswerState Rejected state

### Changed
 - Changed IConference/IParticipant Start/End to StartTime/EndTime for consistency
 - Changed IIncomingCall OnAnswerStateChanged event args to be more generic
 - Changed eCallAnswerState Autoanswer to AutoAnswer

### Removed
 - Removed the call direction from IIncomingCall
 - Removed ConferenceManager Recent Calls

## [15.0.0] - 2020-03-20
### Added
 - Zoom can now take the Device ID of a USB ID and match them with a USB IDs on the camera component
 - Zoom will attempt to set the active USB camera when a camera is routed
 - Zoom volume component and control for output devices
 - Zoom local and far end camera control
 - AbstractParticipant & AbstractTraditionalParticipant
 - Zoom Layout Status querying to determine whether layout controls are available or not.
 - Zoom now has setting for RecordEnalbed, defualting to true
 - Zoom now has setting for DialOutEnabled, defaulting to true
 - ZoomMiddleware exposes a TCP console at listen port + 1 (default 2246)
 - Zoom now has a MuteMyCameraOnStart option, to keep the room's camera disabled when joining a meeting.
 - Zoom now has settings for four originator camera devices and associated USB IDs
 - VolumePoints can be registered with the ConferenceManager for privacy mute
 - ConferenceManager will enforce privacy mute on DSP and Microphone volume points
 - Added common abstractions for web and traditional conferencing
 - IConferenceDeviceControl exposes a SupportedConferenceFeatures property for determining if privacy mute is supported
 - Microphone privacy is enforced as a last resort to avoid complicating AEC
 - Added methods to ConferenceManager for determining if privacy mute is currently available

### Changed
 - Cameras are passed to IVideoConferenceRouteControl when setting active camera
 - ConferencePoints wrap a conference control
 - Reworked Cisco volume control to fit new volume interfaces
 - Fixed IndexOutOfBoundsException in ConferenceManager.RemoveRecentCall(IIncomingCall)
 - Fixed a bug where Zoom would stop presentations while swapping sources
 - Clarified ZoomMiddleware installation steps
 - Zoom now saves MuteParticipantsOnStart setting to XML to persits through program restarts
 - Fixed bugs where the DialingPlan was not correctly resolving call types
 - Fixed a bug where the Zoom layout control was incorrectly determining global layout availability
 - Substantial refactoring of ConferenceManager to split into multiple child classes
 - When Zoom connects to a call lobby that requires a host to start the call, the conference status now properly changes to connected.
 - Splitting Zoom participant added/updated events, fixing bad MuteOnEntry logic
 - Using UTC for times
 - Fixed a bug where Zoom presentation sharing did not properly account for remote participant sharing

## [14.1.1] - 2020-02-21
### Changed
 - Cameras implement StoreHome method

## [14.1.0] - 2019-12-03
### Added
 - Implemented Zoom event for call-out failing to be placed

### Changed
 - Fixed a bug that was preventing ZoomRoom from properly interpreting all-day bookings

## [14.0.0] - 2019-11-18
### Added
 - Conferencing Check In/Check Out support
 - ZoomRoom MuteUserOnEntry API
 - ZoomRoom booking check in API
 - ZoomRoom Traditional Call Component & Control
 - ZoomRoom Audio Component
 - ZoomRoom Layout Component & Control
 - Event for when far end requests ZoomRoom to Unmute video
 - ZoomRoom Allow Participant to record call console command
 - ZoomRoom Call Record console command
 - ZoomRoom Call Lock console command
 - Zoom Call Lock Converter/Response to Call Configuration Converter/Response
 - Zoom Updated Call Record Info Event Converter/Response
 - Added method to IFavorites for getting favorites by dial protocol
 - Added OnIncomingCallAdded and Removed events to conference manager

### Changed
 - Renamed old appearances of "Source" to "Participant", and "SourceType" to "CallType"
 - Substantial refactoring of Zoom device driver to better seperate Controls from Components

## [13.8.1] - 2019-10-08
### Changed
 - Fixed a bug where zoom bookings without a meeting number were not being handled correctly

## [13.8.0] - 2019-10-07
### Added
 - Zoom conference control raises an event when a far end participant requests a mute state change
 - Added an extension method to get the duration of a conference

### Changed
 - Fixed NullReferenceException when handling Zoom camera selection responses
 - Fixed a bug where all-day Google events were being serialized in Zoom as empty strings
 - Fixed a bug where zoom bookings were not being listed when the zoom room was initialized
 - Fixed deadlocks in the Zoom CallComponent
 - Fixed a bug where Zoom contexts would be dialed with an empty password
 - Zoom contact directory optimizations
 - Zoom bookings are initialized when the device initializes

## [13.7.0] - 2019-09-16
### Added
 - ZoomRoomConferenceControl raises all relevant information when a password is requested

### Changed
 - Updated IPowerDeviceControls to use PowerState
 - Fixed a typo in Zoom response serialization

## [13.6.0] - 2019-08-27
### Added
 - Added event handling in Zoom for when the active camera is changed.

## [13.5.0] - 2019-08-15
### Changed
 - Changed ZoomMiddleware default password

## [13.4.0] - 2019-07-16
### Added
 - Added PresentationActive property and event to IPresentationControl
 - Added methods for setting Cisco presentation multi-screen mode
 - Added methods for setting Polycom multi-monitor mode
 
### Changed
 - Fixed an issue with duplicated Zoom participants

## [13.3.1] - 2019-07-16
### Changed
 - Fixed an issue where only incoming calls were being removed and not participants

## [13.3.0] - 2019-07-16
### Added
 - Added ThinDialContext for calendar parsing
 - Added more information to the participant conference control console command.
 - Added incoming call support to Cisco
 - Once the incoming call is answered or ignored remove the incoming call for Cisco and Polycom

### Changed
 - Fix parsing for Zoom SystemInfo
 - Fixed an issue where Zoom meeting participants were being dropped from the meeting on program restart
 - Renamed Dialing Device Control to Conference Device Control

## [13.2.0] - 2019-06-25
### Added
 - Added MeetingNeedsPasswordResponse for Zoom
 - Added MeetingNeedsPasswordResponseConverter
 - Added Event Handler that raises an event when the response is recieved in ZoomRoomConferenceControl
 - Added password support for dialcontexts
 - Added console command for joining a zoom meeting with a password
 - Added Disconnecting to eConferenceStatus enum
 - Added IsHost and IsSelf properties for IWebParticipant
 - Added support for recognizing host changes in Zoom meetings

### Changed
 - Fixing bug where ZoomRoomRoutingControl would return different inputs based on presentation feedback
 - Fixed H.323/SIP devices showing up in Zoom contacts with blank names
 - Zoom switches to disconnecting status before attempting to leave a meeting
 - Zoom Middleware sends all data to clients
 - Fixed Zoom issues with buffering and initialization
 - Include the room as a participant in Zoom meeting participant list

### Removed
 - Removed OnHold as a possible conference status for Zoom conferences

## [13.1.0] - 2019-05-03
### Added
 - Added ConferencePoint originators and settings
 - Added project for Zoom Middleware service
 - Added ZoomLoopbackServerDevice and settings

### Changed
 - Fixed up Polycom driver for conferencing refactor
 - Dialing controls are registered against the ConferenceManager by type
 - Zoom uses JSON converters instead of reflection

## [13.0.1] - 2019-01-29
### Changed
 - Fixed issue with calendar booking de-duplication that was preventing bookings from updating
 - Small Zoom fixes

## [13.0.0] - 2019-01-14
### Changed
 - Significant refactoring to make the distinction between audio, video and web conferencing

## [12.0.0] - 2019-01-10
### Added
 - Added port configuration features to conferencing devices

## [11.10.2] - 2020-08-10
### Changed
 - Fixed Zoom Room API regression where "Dial Start" became broken in Zoom Room 5.1.1 software

## [11.10.1] - 2020-07-28
### Changed
 - CiscoCodecCamera - Storing and activating presets now works properly
 - CiscoCodecCamera - Device online state is now properly updated

## [11.10.0] - 2020-04-30
### Added
 - CiscoCodecDevice - Added properties/parsing for SerialNumber and SoftwareVersionDate
 - CiscoCodecDevice - Added External Telemetry Provider to provide network info, software info, model, and serial number
 - CiscoCodecCamera - Added telemetry for model, serial number, firmware, mac address
 
### Changed
 - CiscoCodecCamera - fixed to show offline if the camera is offline on the Codec

## [11.9.1] - 2020-02-18
### Changed
 - Removed "CamerasMuted" property and assciated method/event - functionality duplicated by MainVideoMuted
 - Changed "MainVideoMute" to "MainVideoMuted" in various places for consistency

## [11.9.0] - 2020-02-14
### Added
 - ConferenceManager-Added new options for EnforceAutoAnswer and EnforceDoNotDisturb, with new option for DoNotEnforce
 - Codec settings have properties for up to 6 inputs
 - Cisco codec now supports MainVideo (camera) mute on the Video Component

### Changed
 - ConferenceManager-Removed the previous AutoAnswer and DoNotDisturb properties/methods, use the new enforce options
 - ConferenceManager-Changed IsAuthoratative functionality to live under IsActive

## [11.8.0] - 2019-11-18
### Added
 - Cisco Codec now has a volume control (id 8) for the main volume level/mute

## [11.7.0] - 2019-08-02
### Added
 - Cisco "Bookings Updated" event is subscribed to and causes bookings to be listed again
 
### Changed
 - No longer matching Cisco bookings by ID

## [11.6.2] - 2019-08-02
### Changed
 - Cisco active camera feedback is now updated correctly

## [11.6.1] - 2019-07-31
### Changed
 - Fixed a bug where cisco dial strings were being malformed for Audio|Video calls
 - Small performance improvement by avoiding precompiling configured regex

## [11.6.0] - 2019-07-16
### Changed
 - Changed PowerOn/PowerOff methods to support new pre-on/off callbacks

## [11.5.3] - 2019-07-02
### Changed
 - Fixed a bug where Polycom would enter a locked state when receiving a multi-line response with no content

## [11.5.2] - 2019-06-13
### Changed
 - Fixed a bug where Cisco SpeakerTrack availability would default to an incorrect value

### Removed
 - Removed errant test logging in the interpretation client and server

## [11.5.1] - 2019-05-30
### Changed
 - Fixed a bug in Cisco booking parsing when a booking has no associated call information

## [11.5.0] - 2019-05-15
### Added
 - Added PresenterTrack and SpeakerTrack items to Cisco Cameras component.
 - Added Cisco RoomAnalytics component
 - Added CiscoCodecOccupancySensorControl
 - Added console features to Cisco and Polycom camera devices
 - Added telemetry for dialers
 - Added SIP telemetry to Cisco

### Changed
 - Better Cisco support for multiple SIP registrations
 - Fixed issues with multiple Cisco cameras fighting for preset assignment

## [11.4.0] - 2019-04-05
### Added
 - IConferenceManager authoritative mode can be turned on and off
 
## [11.3.1] - 2019-02-11
### Changed
 - Fixed bug where Polycom Group Series 500 dial string was being truncated, causing OBTP to fail

## [11.3.0] - 2019-01-16
### Changed
 - Fixed ConferenceManager issue that was yielding audio dialers when requesting a video dialer

## [11.2.0] - 2019-01-02
### Added
 - Added OnProviderAdded and OnProviderRemoved events to IConferenceManager

### Changed
 - Fail more gracefully when a Cisco Camera is added without a parent Cisco Codec

## [11.1.1] - 2018-11-20
### Changed
 - Small performance improvement for Cisco feedback parsing

## [11.1.0] - 2018-11-08
### Added
 - Added far-end zoom for Cisco remote cameras.

### Changed
 - Improved thread safety in Server SimplInterpretationShim
 - Fixed bug where Polycom GroupSeries would not initialize unless getting the SSH welcome message

## [11.0.1] - 2018-10-30
### Changed
 - Fixed loading issue where devices would not fail gracefully when a port was not available

## [11.0.0] - 2018-10-18
### Added
 - Conference manager now enforces privacy mute state on feedback providers
 - Support CE 9.3 for the cisco codec
 - Fallback to root directory folder when getfolder would otherwise fail
 - XML Bookig numbers parsing
 - Added Feedback Resubscription to Polycom
 
### Changed
 - Fixed bug where if the client interpretation device was initialized before the server, it would not connect
 - Overhaul XML parsing to improve performance

## [10.0.2] - 2018-10-04
### Changed
 - Fixed bug where Polycom failed calls would get stuck in "disconnecting" state

## [10.0.1] - 2018-09-25
### Changed
 - Fixed bugs with Polycom initialization commands being cleared prematurely
 - Fixed bug where Polycom call state would bounce between disconnected and connected

## [10.0.0] - 2018-09-14
### Added
 - Added Polycom calendar parsing

### Changed
 - Fixed bug with Cisco VTC awake state
 - Cisco routing control does not depend on feedback from the device
 - Significant routing performance improvements

## [9.1.0] - 2018-07-19
### Added
 - Added Polycom button features for emulating remote control

### Changed
 - Default camera IDs to 1 when deserializing settings and no ID is specified
 - Fixed issues with multiple Polycom conference sources being created
 - ThinConferenceSource SourceTypes specified
 - Reduced spamming Polycom with addressbook commands
 - Polycom contacts are added as a flat list

## [9.0.0] - 2018-07-02
### Added
 - Added Polycom Group Series conferencing device
 - Added Polycom camera device
 
### Changed
 - CiscoCodec console improvements
 - Phonebook directory/folder improvements

## [8.0.0] - 2018-06-19
### Added
 - Added video conferencing abstractions and interfaces for devices and controls 
 - Added functionality to S+ InterpretationServerShim to support S+ requirements
 - Added console commands to InterpretationServerDevice

### Changed
 - Cisco codec driver overhaul to use new abstractions
 - Changed data type for booth ID on interpretation devices to be ushort
 - Fixed potential thread-unsafety in interpretation server
 - ConnectionStateManager now used to maintain RPC connection for InterpretationClient/Server
 - Fixed language setting not propegating to client from server
 - Fixed major issue where only the first adapter for the InterpretationServerDevice would be utilied/transmitted to client
 - Fixed issue where disconnections wouldn't clear the calls from the client, causing them to duplicate when reconnected

## [7.1.0] - 2018-06-04
### Changed
 - CiscoCodec phonebook type is configured via settings
 - Serial devices use ConnectionStateManager for maintaining connection to remote endpoints

## [7.0.0] - 2018-05-24
### Changed
 - Significant Interpretation device refactoring
 
## [6.0.0] - 2018-05-09
### Added
 - Adding Interpretation client and server devices
 - Adding Interpretation shims for S+

### Changed
 - Moved codec input types configuration into new CodecInputTypes class

## [5.2.0] - 2018-05-03
### Added
 - Additional console commands for dialing controls

## [5.1.0] - 2018-04-23
### Added
 - ICD.Connect.Conferencing.Mock project, relevant project files
 - Mock conferencing device + settings, to simulate a conferencing device such as a cisco codec
 - Mock dialing device control, to simulate a dialer
 - Conferencing Client and Server Devices, to allow RPC control of Dialing Controls, and Conferencing Devices
 - Added extension method for determining if a conference source is ringing and incoming

### Changed
 - Make the cisco codec properly update its do not disturb and auto answer values

## [5.0.0] - 2018-04-23
### Changed
 - Removed suffix from assembly name
 - Using API event args
