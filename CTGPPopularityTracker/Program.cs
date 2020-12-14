using System;
using System.Configuration;
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

        private static void Main(string [] args)
        {

            // tracker.GetTracksSortedPopularity().ForEach(kpv => 
            //     Console.WriteLine("Track: {0}, Popularity: {1}", kpv.Key.Item1, kpv.Value));
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            //Start by gathering data, and set this to run every 60 minutes
            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromMinutes(60);

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

        private async Task CmdErroredHandler(CommandsNextExtension _, CommandErrorEventArgs e)
        {
            var failedChecks = ((ChecksFailedException)e.Exception).FailedChecks;
            foreach (var unused in failedChecks)
            {
                await e.Context.RespondAsync("You cannot use this command.");
            }
        }
    }
}
