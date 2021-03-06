# Changelog
All notable changes to this project will be documented in this file.

Versions number is **MAJOR.MINOR.IMPROVEMENT.FIX**

**Major** releases imply a completely new version that can change the whole program in a massive way or represent a milestone.  
**Minor** usually has a new feature or a massive change.  
**Improvements** usually improve already used features and have changes or modifications in a non breaking way or general fixes.  
**Fixes** are small modifications, typos and fast bug fixes.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [v0.7.3.0-b] - 2019-03-05

### Changed

- More Logging is Sent to Sentry
- Project Details, Such Description, Copyright, etc.

### Fixed

- No More Logs will be lost when Crashing or Closing
- If Twitch Responds with a Gateway Timeout it Tries again after a set amount of time
- Fixed Version Changing if Used in Another Project

## [v0.7.2.1-b] - 2019-02-24

### Added

- Sentry Error Reporting

### Fixed

- Clicking on console will no longer stop processing 

## [v0.7.2.0-b] - 2019-02-23

### Added

- Custom Rank Based Welcome Message

### Changed

- Update Rank after a bet ends and people gain or lose points
- Poll creation fails if a poll is already active and sends message why

### Fixed

- Didn't Update Rank if you won at Gamble

## [v0.7.1.0-b] - 2019-02-18

### Changed

- Update rank after gamble

### Fixed

- Can't Gamble negative or 0 points

## [v0.7.0.0-b] - 2019-02-16

### Added

- Bet Command
- Gamble Command

### Changed

- Under the hood Command Structure Improved
- !pointrate renamed to !xprate

## [v0.6.0.0-b] - 2019-01-29
### Changed
- !help command list is dynamically generated
- !comenzi now is !help
- Moderator checks for commands inside get unified
- Logs generated in logs folder
- All original Poll Commands unified under a !poll command
- User and Mod get list only for commands that they can use
- Giveaway doesn't work without item

## [v0.5.4.1-b] - 2019-01-20
- Updated dependencies, Now vip users are read

## [v0.5.4-b] - 2019-01-18
- !manage command now also says the number of hours and points the user currently has
- Commands are now handled better under the hood
- No more Migration Announcement
- PollVote command now uses only the first argument, ignores anything after

## [v0.5.0-b] - 2019-01-07
- Exceptions are better handled and more are passed upwards
- Bot doesn't say hi anymore to own user
- !manage Command logic condensed
- Improved Error Messages across all commands
- More Things in the bot are configurable, preparing things for when a config file will be used
- More Responses are moved to StandardMessages class to String.Format format to be easily managed or loaded from a file in the future
- Different Checks in the Bot have been improved
- Presence, Talkers and Filter performance should be improved for higher numbers of users
- Code cleaned up
- Startup Improved
- PollManager has more states, better handled polls
- Modularized and Separated to more files different pieces of logic
- Better Async => more responsive
- !pointrate, !about, !changelog Commands Added

## [v0.4.17.4-b] - 2018-11-22
- text correction

## [v0.4.17.3-b] - 2018-11-22
- cleaned, fixed announcement

## [v0.4.17-b] - 2018-11-21
- hotfixes of top-order and /me

## [v0.4.16-b] - 2018-11-21
- presence counter, cleanup, improvements, throw upwards more, get more users at once

## [v0.4.10-b] - 2018-11-17
- Improvements: TopCommand, Giveaway(now it announces potential winners),added some case prevention, unit test for dataprocessor Changed the way connections are started

## [v0.4.6-b] - 2018-11-13
- New in this version are top and giveaway commands.

## [v0.4.5-b] - 2018-11-13
- has some instabilites and didn't work out, next release is perfect

## [v0.4.2-b] - 2018-11-09
- took a month for this beta release, this has unit tests

## [v0.3.3-b] - 2018-10-18
- The most stable beta version yet.

## [v0.3.2-b] - 2018-10-16
- just a few visual changes to console

## [v0.3.1-b] - 2018-10-14
- this has a few performance improvements

## [v0.3.0-b] - 2018-10-13
- added logging in this version it seems

## [v0.2.3-b] - 2018-10-13
- first proper versioning of the program as I see

[v0.7.3.0-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.7.3.0-b
[v0.7.2.1-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.7.2.1-b
[v0.7.2.0-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.7.2.0-b
[v0.7.1.0-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.7.1.0-b
[v0.7.0.0-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.7.0.0-b
[v0.6.0.0-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.6.0.0-b
[v0.5.4.1-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.5.4.1-b
[v0.5.4-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.5.4-b
[v0.5.0-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.5.0-b
[v0.4.17.4-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.4.17.4-b
[v0.4.17.3-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.4.17.3-b
[v0.4.17-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.4.17-b
[v0.4.16-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.4.16-b
[v0.4.10-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.4.10-b
[v0.4.6-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.4.6-b
[v0.4.5-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.4.5-b
[v0.4.2-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.4.2-b
[v0.3.3-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.3.3-b
[v0.3.2-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.3.2-b
[v0.3.1-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.3.1-b
[v0.3.0-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.3.0-b
[v0.2.3-b]: https://dev.azure.com/sweethuman/_git/EvilBot?version=GTv0.2.3-b
