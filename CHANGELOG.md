# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
 - Added PresenterTrack and SpeakerTrack items to Cisco Cameras component.

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
