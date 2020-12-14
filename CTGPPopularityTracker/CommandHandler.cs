using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace CTGPPopularityTracker
{
    public class CommandHandler : BaseCommandModule
    {

        //DISCORD CONSTS
        private const string PopularityInfo =
            "*Popularity is calculated using the sum of the popularty of a track in CTGP Revolution Time Trials, along with the number of times the track has been played on WiimmFi in the past month.*";
        private readonly DiscordColor _botEmbedColor = new DiscordColor("#FE0002");

        //FIELDS
        private DiscordChannel _pollStartChannel, _pollDisplayChannel;

        /*
         * SHOW COMMANDS
         */

        [Command("showtop"), Description("Displays the top 10 most popular tracks on CTGP"),
         Cooldown(1, 30, CooldownBucketType.User)]
        public async Task ShowTopCommand(CommandContext ctx)
        {
            //Get the top 10
            var topTen = Program.Tracker.GetSortedListAsString(0, 10);

            //Craft the Embed to the user
            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Description = PopularityInfo,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last updated: {Program.Tracker.LastUpdated:dddd, dd MMMM yyyy, HH:mm UTC}"
                }
            };

            //Add field to embed and send
            embed.AddField("Top 10", topTen);
            await ctx.RespondAsync(null, false, embed);
        }

        [Command("showbottom"), Description("Displays the top 10 least popular tracks on CTGP"),
         Cooldown(1, 30, CooldownBucketType.User)]
        public async Task ShowBottomCommand(CommandContext ctx)
        {
            //Get the top 10
            var bottomTen = Program.Tracker.GetSortedListAsString(Program.Tracker.Tracks.Count, 10, true);

            //Craft the Embed to the user
            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Description = PopularityInfo,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last updated: {Program.Tracker.LastUpdated:dddd, dd MMMM yyyy, HH:mm UTC}"
                }
            };

            //Add field to embed and send
            try
            {
                embed.AddField("Bottom 10", bottomTen);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            await ctx.RespondAsync(null, false, embed);
        }

        [Command("showtopbottom"), Description("Displays the top 10 most and least popular tracks on CTGP"),
         Cooldown(1, 30, CooldownBucketType.User)]
        public async Task ShowTopAndBottomCommand(CommandContext ctx)
        {
            //Get the top 10 and bottom 10 
            var topTen = Program.Tracker.GetSortedListAsString(0, 10);
            var bottomTen = Program.Tracker.GetSortedListAsString(Program.Tracker.Tracks.Count, 10, true);

            //Craft the Embed to the user
            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Description = PopularityInfo,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last updated: {Program.Tracker.LastUpdated:dddd, dd MMMM yyyy, HH:mm UTC}"
                }
            };

            //Add fields to embed
            embed.AddField("Top 10", topTen, true);
            embed.AddField("Bottom 10", bottomTen, true);
            await ctx.RespondAsync(null, false, embed);
        }

        [Command("show"), Description("Lists tracks from a specific starting point down to the next x amount (x being no larger than 25)"),
         Cooldown(1, 30, CooldownBucketType.User)]
        public async Task ShowTracksFromSpecificSetCommand(CommandContext ctx, 
            [Description("The starting point of the search")]int startPoint, 
            [Description("The amount of tracks you want to list")]int count)
        {
            if (count > 25 || count < 2)
            {
                await ctx.RespondAsync(
                    "Please adjust your track count to be between 2 and 25 inclusive");
                return;
            }

            if (startPoint < 1 || startPoint > Program.Tracker.Tracks.Count)
            {
                await ctx.RespondAsync(
                    $"The starting point has to be between 1 and {Program.Tracker.Tracks.Count} inclusive");
                return;
            }

            if (startPoint + count > Program.Tracker.Tracks.Count)
            {
                count = Program.Tracker.Tracks.Count - startPoint + 1;
            }

            //Get list
            var tracks = Program.Tracker.GetSortedListAsString(startPoint - 1, count);
            if (tracks == null)
            {
                await ctx.RespondAsync(
                    "Your starting point is larger than the amount of tracks available. Try again with a " +
                    "smaller start point.");
                return;
            }

            //Craft the Embed to the user
            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Description = PopularityInfo,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last updated: {Program.Tracker.LastUpdated:dddd, dd MMMM yyyy, HH:mm UTC}"
                }
            };

            //Add field to embed and send
            embed.AddField("Custom range", tracks);
            await ctx.RespondAsync(null, false, embed);
        }

        [Command("getpopularity"), Description("Finds the popularity of tracks which share the same search parameter."),
         Cooldown(1, 10, CooldownBucketType.User)]
        public async Task GetTracksPopularityCommand(CommandContext ctx,
            [RemainingText, Description("The search parameter")]
            string param)
        {
            //Get the list of tracks in order
            var tracks = Program.Tracker.FindTracksBasedOnParameter(param);

            //Craft the Embed to the user
            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Description = PopularityInfo,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last updated: {Program.Tracker.LastUpdated:dddd, dd MMMM yyyy, HH:mm UTC}"
                }
            };

            //Add fields to embed
            embed.AddField($"Tracks containing \"{param}\"", tracks);
            await ctx.RespondAsync(null, false, embed);
        }

        /*
         * POLL COMMANDS
         */

        [Command("pollsetup"), Description("Runs the setup for PopularityBot polling."),
         RequirePermissions(Permissions.Administrator)]
        public async Task RunPollSetupCommand(CommandContext ctx)
        {
            //Start by asking for a channel to send poll messages to
            await ctx.RespondAsync(
                "Hi there! Firstly I need to ask you for a channel for me to " +
                "direct my poll messages to. Please respond with the channel name you want to use.");


            var successful = false;
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (!successful)
            {
                await ctx.Message.GetNextMessageAsync(m =>
                {
                    var channel = ctx.Guild.Channels.FirstOrDefault(x =>
                        x.Value.Type == ChannelType.Text && string.Equals(x.Value.Name, m.Content,
                            StringComparison.CurrentCultureIgnoreCase)).Value;

                    if (channel == null)
                    {
                        ctx.RespondAsync("I couldn't find that text channel, try again.");
                        return false;
                    }

                    _pollDisplayChannel = channel;
                    successful = true;
                    return true;

                });
            }

            successful = false;

            await ctx.RespondAsync(
                "Awesome. Now tell me the channel name that you wish to start polls from. " +
                "Ideally this would be protected and only accessible by certain users in the server.");

            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (!successful)
            {
                await ctx.Message.GetNextMessageAsync(m =>
                {
                    var channel = ctx.Guild.Channels.FirstOrDefault(x =>
                        x.Value.Type == ChannelType.Text && string.Equals(x.Value.Name, m.Content,
                            StringComparison.CurrentCultureIgnoreCase)).Value;

                    if (channel == null)
                    {
                        ctx.RespondAsync("I couldn't find that text channel, try again.");
                        return false;
                    }

                    _pollStartChannel = channel;
                    successful = true;
                    return true;

                });
            }

            await ctx.RespondAsync("All done! You have successfully set up polling in PopularityBot. Have fun!");

        }

        [Command("startpoll"), Description("Starts the process of starting a poll.")]
        public async Task StartPollCommand(CommandContext ctx)
        {
            string[] squareBoxes = { ":red_square:", ":green_square:", ":blue_square:", ":yellow_square:", 
                ":brown_square:", ":purple_square:", ":orange_square:" };
            var dayCount = int.MaxValue;

            //Check they are in the right channel, and if not exit
            if (ctx.Channel != _pollStartChannel)
            {
                await ctx.RespondAsync("You do not have permission to start a poll here.");
                return;
            }

            await ctx.RespondAsync(
                "You will now be asked a series of questions in which the poll will be generated from.");

            //First question: Ask for a description as to why the poll is being conducted.
            InteractivityResult<DiscordMessage> resultQ1;
            await ctx.RespondAsync(
                "*Question 1: Please provide a small description as to why you are running this poll (max 300 characters).*");

            do
            {
                resultQ1 = await ctx.Message.GetNextMessageAsync(m => m.Content.Length <= 300);
            } while (resultQ1.TimedOut || string.IsNullOrEmpty(resultQ1.Result.Content));

            var descriptionPoll = resultQ1.Result?.Content;

            //Second question: Ask what tracks are to be included, split by commas, max 7
            InteractivityResult<DiscordMessage> resultQ2;

            await ShowTopAndBottomCommand(ctx);
            await ctx.RespondAsync(
                "*Question 2: Please select up to 7 tracks you wish to include in the poll. Above are the current top 10 " +
                "and bottom 10 tracks, which may be helpful to you. Please list each track with a comma between them.*");

            do
            {
                resultQ2 = await ctx.Message.GetNextMessageAsync(m => m.Content.Split(',').Length <= 7);
            } while (resultQ1.TimedOut || string.IsNullOrEmpty(resultQ2.Result.Content));

            var trackList = resultQ2.Result.Content.Split(',');

            //Third question: Duration of the poll
            await ctx.RespondAsync("*Question 3: How long do you want the poll to last in days? (max 7)*");

            do
            {
                await ctx.Message.GetNextMessageAsync(m => int.TryParse(m.Content, out dayCount));
                // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            } while (dayCount > 7 && dayCount < 1);

            //Show them the details and ask if they want to start the poll
            InteractivityResult<DiscordMessage> resultFinal;

            await ctx.RespondAsync(
                "*Here is the details of the poll. If you are happy, respond \"yes\", otherwise respond " +
                "\"no\" to cancel it.*");
            await ctx.Channel.SendMessageAsync(
                $"**Description:** {descriptionPoll}\n**Tracks:** {string.Join(',', trackList)}\n**Number of days:** {dayCount}");

            do
            {
                resultFinal = await ctx.Message.GetNextMessageAsync(m => 
                    m.Content.ToLower().Equals("yes") || m.Content.ToLower().Equals("no"));
            } while (resultQ1.TimedOut || string.IsNullOrEmpty(resultFinal.Result.Content));

            if (resultFinal.Result.Content.ToLower().Equals("no")) return;

            //TIME TO SUBMIT THE POLL

            //Start by building the embed field, and add them to a list for easy ordering
            var sb = new StringBuilder();
            var trackEmojiPairs = new Dictionary<DiscordEmoji, string>();

            for (var i = 0; i < trackList.Length; i++)
            {
                var emoji = DiscordEmoji.FromName(ctx.Client, squareBoxes[i]);
                trackEmojiPairs.Add(emoji, trackList[i]);
                sb.Append($"{emoji}\t{trackList[i]}\n");
            }

            var fieldString = sb.ToString().Substring(0, sb.Length - 1);

            //Build and submit the poll, then wait!
            var pollEmbed = new DiscordEmbedBuilder()
            {
                Color = _botEmbedColor,
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    Name = $"{ctx.Message.Author.Username} has started a poll!",
                    IconUrl = ctx.Message.Author.AvatarUrl
                },
                Description = $"*\"{descriptionPoll}\"*",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"This poll will end on {DateTime.UtcNow.AddDays(dayCount):dddd, dd MMMM yyyy, HH:mm UTC}"
                }
            };

            pollEmbed.AddField("Tracks to vote on", fieldString);
            var pollMessage = await _pollDisplayChannel.SendMessageAsync(null, false, pollEmbed);

            //Collect the reactions over the time period
            var reactions = await pollMessage.CollectReactionsAsync(TimeSpan.FromDays(dayCount));

            //Once time is up, print the reactions
            sb.Clear();
            foreach (var (key, value) in trackEmojiPairs)
            {
                var reaction = reactions.FirstOrDefault(x => x.Emoji == key);
                sb.Append(reaction != null ? $"{value} - **{reaction.Total}**\n" : $"{value} - **0**\n");
            }


            fieldString = sb.ToString().Substring(0, sb.Length - 1);

            var pollResultsEmbed = new DiscordEmbedBuilder()
            {
                Color = _botEmbedColor,
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    Name = $"{ctx.Message.Author.Username}'s poll has ended!",
                    IconUrl = ctx.Message.Author.AvatarUrl
                }
            };

            pollResultsEmbed.AddField("Tracks that were voted on", fieldString);
            await _pollDisplayChannel.SendMessageAsync(null, false, pollResultsEmbed);
        }
    }
}
