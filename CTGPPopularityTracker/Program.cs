using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTGPPopularityTracker.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;

namespace CTGPPopularityTracker
{
    public class Program
    {
        public static PopularityTracker Tracker = new PopularityTracker();
        public static WikiHandler Wiki = new WikiHandler();
        private static readonly EventHandler eh = new EventHandler();
        private static string SettingsFile;

        private static void Main(string [] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            //Ask to locate the settings file to be used for polling
            do
            {
                Console.WriteLine("Where is the poll settings file located: ");
                SettingsFile = Console.ReadLine();
            } while (!File.Exists(@$"{SettingsFile}"));

            var popularityTimer = new System.Threading.Timer(async (e) =>
            {
                // Try to get new statistics, and if it fails don't update
                try
                {
                    var wiki = new WikiHandler();
                    await wiki.GetWikiTrackList();
                    Wiki = wiki;

                    var track = new PopularityTracker();
                    await track.UpdatePopularity();
                    Tracker = track;
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(55));

            //Sort out Discord Bot stuff
            var discord = new DiscordClient(new DiscordConfiguration
            {
                Token = ConfigurationManager.AppSettings["secret"],
                TokenType = TokenType.Bot
            });

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] {"!"}
            });
            
            commands.RegisterCommands<CommandHandler>();
            commands.CommandErrored += eh.OnCommandError;
            commands.SetHelpFormatter<CustomHelpFormatter>();


            discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis
            });

            await discord.ConnectAsync();

            var discordStatusTimer = new System.Threading.Timer(async (e) =>
            {
                var memberCount = discord.Guilds.Values.Sum(x => x.MemberCount);
                var memberPart = memberCount > 1 ? $"{memberCount} members" : "1 member";
                var serverPart = discord.Guilds.Count > 1 ? $"{discord.Guilds.Count} servers" : "1 server";

                var activity = new DiscordActivity()
                {
                    Name = $"{memberPart} in {serverPart}",
                    ActivityType = ActivityType.ListeningTo,
                };

                await discord.UpdateStatusAsync(activity);
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

            await Task.Delay(-1);
        }

        /// <summary>
        /// Finds poll settings for a specific guild.
        /// </summary>
        /// <param name="guild">The guild id</param>
        /// <returns>The poll settings for that guild in the form of an array.</returns>
        public static ulong[] GetPollSettings(ulong guild)
        {
            //Load the settings file
            var f = new StreamReader(@$"{SettingsFile}");
            string line;

            while ((line = f.ReadLine()) != null)
            {
                //Split and see if the guilds match
                var pollAttr = line.Split(',').Select(ulong.Parse).ToArray();
                if (pollAttr[0] == guild) return pollAttr;
            }

            return new ulong[]{0,0,0};
        }

        /// <summary>
        /// Writes settings to the poll settings file.
        /// </summary>
        /// <param name="line">The settings to add for a specific guild.</param>
        /// <param name="guild">The guild you want to overwrite settings for.</param>
        public static void WritePollSettings(string line, ulong guild = 0)
        {
            var arrSettings = File.ReadAllLines(@$"{SettingsFile}");

            if (arrSettings.Length > 0)
            {
                for (var i = 0; i < arrSettings.Length; i++)
                {
                    if (arrSettings[i].Contains(guild.ToString())) arrSettings[i] = line;
                    File.WriteAllLines(@$"{SettingsFile}", arrSettings);
                }
            } else File.AppendAllText(@$"{SettingsFile}", $"{line}\n");


        }
    }
}
