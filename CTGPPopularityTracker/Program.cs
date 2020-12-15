using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CTGPPopularityTracker
{
    public class Program
    {
        public static PopularityTracker Tracker = new PopularityTracker();
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


            //Start by gathering data, and set this to run every 60 minutes
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(30);

            var timer = new System.Threading.Timer(async (e) =>
            {
                var temp = new PopularityTracker();
                await temp.UpdatePopularity();
                Tracker = temp;
            }, null, startTimeSpan, periodTimeSpan);

            //Sort out Discord Bot stuff
            var discord = new DiscordClient(new DiscordConfiguration
            {
                Token = ConfigurationManager.AppSettings["secret"],
                TokenType = TokenType.Bot
            });

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] {"!"},
            });

            commands.RegisterCommands<CommandHandler>();

            discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis
            });

            await discord.ConnectAsync();
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

            return null;
        }

        public static void WritePollSettings(string line)
        {
            File.AppendAllText(@$"{SettingsFile}", $"{line}\n");
        }
    }
}
