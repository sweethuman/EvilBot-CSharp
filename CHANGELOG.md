# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## Unreleased
### Changed
- !commands command list is dynamically generated
- Moderator checks for commands inside get unified
- Logs generated in logs folder
- All original Poll Commands unified under a !poll command

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
