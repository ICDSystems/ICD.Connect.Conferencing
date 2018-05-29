# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
