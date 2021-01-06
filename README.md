<p align="center"><img src="https://i.imgur.com/5Pkx6yB.png" width="150" height="150"></p>
<p align="center"><img src="https://img.shields.io/github/workflow/status/rhys-wootton/PopularityBot/.NET%20Core"> <img src="https://img.shields.io/github/license/rhys-wootton/PopularityBot"> <img src="https://img.shields.io/github/repo-size/rhys-wootton/PopularityBot"> <img src="https://img.shields.io/tokei/lines/github/rhys-wootton/PopularityBot"><br><a href="http://bit.ly/PopularityBot"><img src="https://img.shields.io/static/v1?&label=discord&message=add%20to%20server&color=7289DA"></a></p>

PopularityBot is a Discord bot that tracks the changes in popularity of tracks that are currently in Mario Kart Wii, and its #1 mod pack [CTGP Revolution](https://www.chadsoft.co.uk/). It was created to remove the struggles of seeing how popular certain tracks are, and has built in polling functionality to allows users to vote on tracks. It also comes with an ability to search for tracks on the [Custom Mario Kart Wiiki](http://wiki.tockdom.com/wiki/Main_Page).

## How it works

PopularityBot works by using two datasets, the [CTGP Revolution Time Trial API](http://tt.chadsoft.co.uk/index.json), and the [WiimmFi weekly CTWW statistics page](https://wiimmfi.de/stats/track/wv/ctgp). The flow of getting the popularity of each track is as follows:

1. Every hour, PopularityBot will connect to the CTGP Revolution Time Trial API and grab the data from the their respective sections (`original-track-leaderboards.json` and `ctgp-leaderboard.json`.
2. PopularityBot will then store the track’s name and SHA1 hash, along with its popularity score, in a `HashMap`, which allows PopularityBot to map a unique track to a popularity value.
3. PopularityBot then knows all tracks that are in CTGP Revolution, along with the 32 base game tracks, and begins to connect to two WiimmFi statistics page, the [Nintendo tracks statistics](https://wiimmfi.de/stats/track/wv/all?p=std,c0,0) and the [CTGP Revolution tracks statistics](https://wiimmfi.de/stats/track/wv/ctgp?p=std,c0,0).
4. Once connected, it scans through each row, seeing if there is a name match in the existing `HashMap` created from the Time Trial API, using the SHA1 hash.
      1. If there is a match, [a formula is run to determine its popularity score](https://docs.google.com/document/d/1C8grliYKX-d5vtrzCJ8DM1oAyANC2sTTfOzBlJeMzaQ/edit?usp=sharing).
      2. If there is no match, the track is not in CTGP Revolution, so we skip it.

## Commands

### Popularity Commands

`!nin` commands relate to the base game tracks in Mario Kart Wii. `!ctgp` commands relate to the tracks in CTGP Revolution.
* `!nintop` & `!ctgptop` : Shows the top 10 tracks in ascending order of popularity.
* `!ninbottom` & `!ctgpbottom` : Shows the bottom 10 tracks in descending order of popularity.
* `!nintopbottom` & `!ctgptopbottom` :  Combines the results from the previous two commands into one.
* `!ninlist/ctgplist <starting_point> <ending_point>` Lists the number of tracks from the starting point specified in ascending order of popularity. The number of tracks has to be between 2 and 25 inclusive.
* `!ninfind/ctgpfind <search_param>` Lists all tracks containing the search parameter in ascending order of popularity. It stops listing tracks after a 25th track has been found.

All these commands have overloads for only showing popularity based on Time Trials and WiimmFi popularity. This is achieved by adding `wf` or `tt` at the end of each command.

### Poll Commands

* `!pollsetup` Sets up the polling capabilities of the bot. This command can only be run by a server administrator.
* `!startpoll` Starts the process of creating a poll. It will ask a few questions relating to the creation of the poll, and once all questions have been answered, a poll is started.

### Other Commands
* `!wiki` Sends details of a custom track from the [Custom Mario Kart Wiiki](http://wiki.tockdom.com/wiki/Main_Page).
* `!github` Sends a link to the GitHub repo for PopularityBot.
* `!donate` Sends a message on how to donate to me to help this project and others I make.

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

* [DSharpPlus: A .NET Standard library for making bots using the Discord API](https://github.com/DSharpPlus/DSharpPlus)
* [Html Agility Pack](https://html-agility-pack.net/)
* [System.Configuration.ConfiguarionManager](https://www.nuget.org/packages/System.Configuration.ConfigurationManager/5.0.0?_src=template)

## Current Issues

* If the bot goes down for any reason, and is restarted, active polls will no longer be concluded.
  * This can be remedied by either starting a new poll and having users vote again, or count up the results manually when the original poll would have ended.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

