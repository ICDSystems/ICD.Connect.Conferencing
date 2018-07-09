# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Changed
 - Default camera IDs to 1 when deserializing settings and no ID is specified

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
