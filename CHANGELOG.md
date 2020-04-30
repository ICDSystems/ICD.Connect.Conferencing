# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
