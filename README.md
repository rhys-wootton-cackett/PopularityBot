# PopularityBot

![GitHub Workflow Status](https://img.shields.io/github/workflow/status/rhys-wootton/PopularityBot/.NET%20Core) ![GitHub](https://img.shields.io/github/license/rhys-wootton/PopularityBot) ![GitHub repo size](https://img.shields.io/github/repo-size/rhys-wootton/PopularityBot) ![Lines of code](https://img.shields.io/tokei/lines/github/rhys-wootton/PopularityBot)

PopularityBot is a Discord bot that tracks the changes in popularity of tracks that are currently in [CTGP Revolution](https://www.chadsoft.co.uk/). It was created to remove the struggles of seeing how popular certain tracks are, and has built in polling functionality to allows users to vote on tracks.

## How it works

PopularityBot works by using two datasets, the [CTGP Revolution Time Trial API](http://tt.chadsoft.co.uk/index.json), and the [WiimmFi CTWW statistics page](https://wiimmfi.de/stats/track/mv/ctgp). The flow of getting the popularity of each track is as follows:

1. Every hour, PopularityBot will connect to the CTGP Revolution Time Trial API and grab the data from the `ctgp-leaderboards` section.
2. PopularityBot will then store the track’s name and SHA1 hash, along with its popularity score, in a `HashMap`, which allows PopularityBot to map a unique track to a value.
3. PopularityBot then knows all tracks that are in CTGP Revolution and begins to connect to the WiimmFi statistics page.
4. Once connected, it scans through each row, seeing if there is a name match in the existing `HashMap` created from the Time Trial API, using either the track name or the SHA1 hash in that row.
   1. If there is a match, the value stored in the first column next to the name, which shows how many times the track has played in the current month, is added to the existing popularity score.
   2. If there is no match, PopularityBot loads the track’s unique URL from ct.wiimm.de, and compares the hashes on that page with the one stored in the `HashMap`.
      1. If there is a match, the value on the main statistics page is added to the existing popularity score (like above.)
      2. If there is no match, the track is not in CTGP Revolution, so we skip it.

## Commands

* `!showtop` Shows the top 10 tracks in ascending order of popularity.
* `!showbottom` Shows the bottom 10 tracks in descending order of popularity.
* `!showtopbottom` Combines the results from the previous two commands into one.
* `!show <starting_point> <number_of_tracks>` Lists the number of tracks from the starting point specified in ascending order of popularity. The number of tracks has to be between 2 and 25 inclusive.
* `!getpopularity <search_param>` Lists all tracks containing the search parameter in ascending order of popularity. It stops listing tracks after a 25th track has been found.
* `!pollsetup` Sets up the polling capabilities of the bot. This command can only be run by a server administrator.
* `!startpoll` Starts the process of creating a poll. It will ask a few questions relating to the creation of the poll, and once all questions have been answered, a poll is started.

## Building From Source

1. Clone the project and load in Visual Studio.

2. Create a new application in the [Discord Developer Portal](https://discord.com/developers/applications) and add a new bot to that application.

3. Create an `App.config` file in the same folder as the `.cs` files, and paste the following:

   ```xml
   <?xml version="1.0" encoding="utf-8" ?>
   <configuration>
     <appSettings>
       <add key="secret" value="[BOT_TOKEN_GOES_HERE]"/>
     </appSettings>
   </configuration>
   ```

4. Build and run the project

## Dependencies

* [DSharpPlus: A .NET Standard library for making bots using the Discord API. (github.com)](https://github.com/DSharpPlus/DSharpPlus)
* [Html Agility Pack](https://html-agility-pack.net/)

## Current Issues

* If the bot goes down for any reason, and is restarted, it will not remember the channels that allow for polls to be sent to and started from.
  * This can be remedied by running `!pollsetup` again.
* If the bot goes down for any reason, and is restarted, active polls will no longer be concluded.
  * This can be remedied by either starting a new poll and having users vote again, or count up the results manually when the original poll would have ended.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

