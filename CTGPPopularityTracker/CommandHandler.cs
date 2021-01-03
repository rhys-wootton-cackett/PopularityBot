using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Linq.Expressions;
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
        private const string DonateInfo =
            "Hosting this bot costs me money (around $5 a month using DigitalOceans VPS), and whilst I can afford it at the moment, help from people that like and use the bot would be super helpful. All " +
            "donations would be put straight into the DigitalOcean account, so you can feel safe knowing that the money does go directly to support the bot's hosting and development, along with helping me " +
            "work on other project to do with CTGP. Don't feel as though you need to donate, but any amount if you can donate would be very appreciated :)";
        private readonly DiscordColor _botEmbedColor = new DiscordColor("#FE0002");

        /*
         * LINK COMMANDS
         */
        [Command("github"), Description("Sends a link to my GitHub page"), Cooldown(5, 50, CooldownBucketType.User)]
        public async Task SendGitHubLinkCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Title = "Check out my source code!",
                Url = "https://github.com/rhys-wootton/PopularityBot",
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                {
                    Url = "https://github.githubassets.com/images/modules/logos_page/GitHub-Mark.png",
                    Height = 20,
                    Width = 20
                }
            };

            await ctx.RespondAsync(null, false, embed);
        }

        [Command("donate"),
         Description(
             "Sends donation links to help support the development of the bot and other things I (RhysRah) create."),
         Cooldown(5, 50, CooldownBucketType.User)]
        public async Task SendDonationLinksCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Title = "Everything helps!",
                Description = DonateInfo,
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = $"❤️ Thanks for the support!"
                }
            };

            embed.AddField("GitHub Sponsor",
                "https://github.com/sponsors/rhys-wootton \nThis is a monthly subscription that helps support the development of all my open source projects, including PopularityBot!");
            embed.AddField("Ko Fi",
                "https://ko-fi.com/rhyswootton \nThis is one time donation that helps support the development of all my open source projects, including PopularityBot!");

            await ctx.RespondAsync(null, false, embed);
        }

        /*
         * EXPLAIN COMMANDS
         */

        [Command("explainpop"), Description("Explains how popularity is calculated."), Cooldown(5, 50, CooldownBucketType.User)]
        public async Task ExplainPopularityCommand(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync("https://docs.google.com/document/d/1C8grliYKX-d5vtrzCJ8DM1oAyANC2sTTfOzBlJeMzaQ/edit?usp=sharing");
        }

        /*
         * SHOW COMMANDS
         */

        [Command("showtop"), Description("Displays the top 10 most popular tracks on CTGP"), Cooldown(5, 50, CooldownBucketType.User)]
        public async Task ShowTopCommand(CommandContext ctx, [Description("OPTIONAL: Sort by Time Trial (tt) or WiimmFi (wf)")]string sortBy = null)
        {
            await ctx.TriggerTypingAsync();

            //Get the top 10, and if an option to sort by was sent, use that
            var topTen = Program.Tracker.GetSortedListAsString(0, 10, false, sortBy);

            //Craft the Embed to the user
            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last updated"
                },
                Timestamp = Program.Tracker.LastUpdated
            };

            //Add field to embed and send
            var embedTitle = sortBy switch
            {
                "tt" => "Top 10 (Time Trial only)",
                "wf" => "Top 10 (WiimmFi only)",
                _ => "Top 10"
            };

            embed.AddField(embedTitle, topTen);
            await ctx.RespondAsync(null, false, embed);
        }

        [Command("showbottom"), Description("Displays the top 10 least popular tracks on CTGP"), Cooldown(5, 50, CooldownBucketType.User)]
        public async Task ShowBottomCommand(CommandContext ctx, [Description("OPTIONAL: Sort by Time Trial (tt) or WiimmFi (wf)")]string sortBy = null)
        {
            await ctx.TriggerTypingAsync();

            //Get the top 10
            var bottomTen = Program.Tracker.GetSortedListAsString(Program.Tracker.Tracks.Count, 10, true, sortBy);

            //Craft the Embed to the user
            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last updated"
                },
                Timestamp = Program.Tracker.LastUpdated
            };

            //Add field to embed and send
            var embedTitle = sortBy switch
            {
                "tt" => "Bottom 10 (Time Trial only)",
                "wf" => "Bottom 10 (WiimmFi only)",
                _ => "Bottom 10"
            };

            embed.AddField(embedTitle, bottomTen);
            await ctx.RespondAsync(null, false, embed);
        }

        [Command("showtopbottom"), Description("Displays the top 10 most and least popular tracks on CTGP"), Cooldown(5, 50, CooldownBucketType.User)]
        public async Task ShowTopAndBottomCommand(CommandContext ctx, [Description("OPTIONAL: Sort by Time Trial (tt) or WiimmFi (wf)")]string sortBy = null)
        {
            await ctx.TriggerTypingAsync();

            //Get the top 10 and bottom 10 
            var topTen = Program.Tracker.GetSortedListAsString(0, 10, false, sortBy);
            var bottomTen = Program.Tracker.GetSortedListAsString(Program.Tracker.Tracks.Count, 10, true, sortBy);

            //Craft the Embed to the user
            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last updated"
                },
                Timestamp = Program.Tracker.LastUpdated
            };

            //Add fields to embed
            var embedTitle = sortBy switch
            {
                "tt" => new[] { "Top 10 (Time Trial only)", "Bottom 10 (Time Trial only)" },
                "wf" => new[] { "Top 10 (WiimmFi only)", "Bottom 10 (WiimmFi only)" },
                _ => new [] { "Top 10", "Bottom 10" }
            };

            embed.AddField(embedTitle[0], topTen, true);
            embed.AddField(embedTitle[1], bottomTen, true);
            await ctx.RespondAsync(null, false, embed);
        }

        [Command("show"), Description("Lists tracks from a specific starting point down to the next x amount (x being no larger than 25)"), Cooldown(5, 50, CooldownBucketType.User)]
        public async Task ShowTracksFromSpecificSetCommand(CommandContext ctx, 
            [Description("The starting point of the search")]int startPoint, 
            [Description("The amount of tracks you want to list")]int count,
            [Description("OPTIONAL: Sort by Time Trial (tt) or WiimmFi (wf)")]string sortBy = null)
        {
            await ctx.TriggerTypingAsync();

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
            var tracks = Program.Tracker.GetSortedListAsString(startPoint - 1, count, false, sortBy);
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
                Footer = new DiscordEmbedBuilder.EmbedFooter { Text = $"Last updated" },
                Timestamp = Program.Tracker.LastUpdated
            };

            //Add field to embed and send
            var embedTitle = sortBy switch
            {
                "tt" => "Custom range (Time Trial only)",
                "wf" => "Custom range (WiimmFi only)",
                _ => "Custom range"
            };
            embed.AddField(embedTitle, tracks);
            await ctx.RespondAsync(null, false, embed);
        }

        /*
         * FIND COMMANDS
         */

        [Command("find"), Description("Finds the popularity of tracks which share the same search parameter."), Cooldown(5, 50, CooldownBucketType.User)]
        public async Task GetTracksPopularityCommand(CommandContext ctx,
            [RemainingText, Description("The search parameter, followed by optional sort type (tt or wf)")] string param)
        {
            await ctx.TriggerTypingAsync();

            //If "wf" or "tt" is the last parameter then use them
            var paramInput = param.Split(" ");

            //Get tracks in order based on popularity
            var tracks = paramInput[^1] switch
            {
                "tt" => Program.Tracker.FindTracksBasedOnParameter(param.Substring(0, param.Length - 3), "tt"),
                "wf" => Program.Tracker.FindTracksBasedOnParameter(param.Substring(0, param.Length - 3), "wf"),
                _ => Program.Tracker.FindTracksBasedOnParameter(param)
            };

            //Craft the Embed to the user
            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Footer = new DiscordEmbedBuilder.EmbedFooter { Text = $"Last updated" },
                Timestamp = Program.Tracker.LastUpdated
            };

            //Add fields to embed
            var embedTitle = paramInput[^1] switch
            {
                "tt" => $"Tracks containing \"{param.Substring(0, param.Length - 3)}\" (Time Trial only)",
                "wf" => $"Tracks containing \"{param.Substring(0, param.Length - 3)}\" (WiimmFi only)",
                _ => $"Tracks containing \"{param}\""
            };
            embed.AddField(embedTitle, tracks);
            await ctx.RespondAsync(null, false, embed);
        }

        [Command("wiki"), Description("Finds the popularity of tracks which share the same search parameter."), Cooldown(5, 50, CooldownBucketType.User)]
        public async Task GetTrackWikiLinkCommand(CommandContext ctx,
            [RemainingText, Description("The search parameter")]
            string param)
        {
            await ctx.TriggerTypingAsync();

            var tracks = Program.Tracker.FindWikiTracksBasedOnParameter(param);

            //Craft the Embed to the user
            var embed = new DiscordEmbedBuilder
            {
                Color = _botEmbedColor,
                Title = "Custom Mario Kart Wiiki",
                Description = "A track may have multiple versions by multiple authors. If so, the link will take you to a page showcasing all versions available.",
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                {
                    Url = "http://wiki.tockdom.com/w/skins/common/images/ct-wiki.png"
                }
            };

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
            await ctx.TriggerTypingAsync();

            var pollSB = new StringBuilder();
            pollSB.Append(ctx.Guild.Id + ",");

            //Start by asking for a channel to send poll messages to
            var embedQuestionnaire = new DiscordEmbedBuilder();
            embedQuestionnaire.Description = "Hi there! Firstly I need to ask you for a channel for me to " +
                                             "direct my poll messages to. Please respond with the channel name you want to use.";
            await ctx.RespondAsync(null, false, embedQuestionnaire);
            
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
                        ctx.RespondAsync(null, false, new DiscordEmbedBuilder().WithDescription("I couldn't find that text channel, try again."));
                        return false;
                    }

                    pollSB.Append(channel.Id + ",");
                    successful = true;
                    return true;

                });
            }

            successful = false;

            await ctx.TriggerTypingAsync();

            embedQuestionnaire.Description =
                "Awesome. Now tell me the channel name that you wish to start polls from. " +
                "Ideally this would be protected and only accessible by certain users in the server.";
            await ctx.RespondAsync(null, false, embedQuestionnaire);

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
                        ctx.RespondAsync(null, false, new DiscordEmbedBuilder().WithDescription("I couldn't find that text channel, try again."));
                        return false;
                    }

                    pollSB.Append(channel.Id);
                    successful = true;
                    return true;

                });
            }

            //Save the settings
            await ctx.TriggerTypingAsync();

            Program.WritePollSettings(pollSB.ToString(), ctx.Guild.Id);
            embedQuestionnaire.Description =
                "All done! You have successfully set up polling in PopularityBot. Have fun!";
            await ctx.RespondAsync(null, false, embedQuestionnaire);

        }

        [Command("startpoll"), Description("Starts the process of starting a poll."), RequirePollStartChannel]
        public async Task StartPollCommand(CommandContext ctx)
        {
            string[] squareBoxes = { ":red_square:", ":green_square:", ":blue_square:", ":yellow_square:", 
                ":brown_square:", ":purple_square:", ":orange_square:" };
            var dayCount = int.MaxValue;

            //Load the right settings for the server
            await ctx.TriggerTypingAsync();
            var pollSettings = Program.GetPollSettings(ctx.Guild.Id);

            //First question: Ask for a description as to why the poll is being conducted.
            InteractivityResult<DiscordMessage> resultQ1;
            await ctx.RespondAsync(null, false, 
                new DiscordEmbedBuilder().WithDescription(
                    "**Question 1:** Please provide a small description as to why you are running this poll (max 300 characters)."));

            do
            {
                resultQ1 = await ctx.Message.GetNextMessageAsync(m => m.Content.Length <= 300);
            } while (resultQ1.TimedOut || string.IsNullOrEmpty(resultQ1.Result.Content));

            var descriptionPoll = resultQ1.Result?.Content;

            //Second question: Ask what options are to be included, split by commas, max 7
            await ctx.TriggerTypingAsync();
            InteractivityResult<DiscordMessage> resultQ2;

            await ctx.RespondAsync(null, false,
                new DiscordEmbedBuilder().WithDescription("**Question 2:** You have a maximum 7 options to add to the poll. Please separate each option with a comma."));

            do
            {
                resultQ2 = await ctx.Message.GetNextMessageAsync(m => m.Content.Split(',').Length <= 7);
            } while (resultQ1.TimedOut || string.IsNullOrEmpty(resultQ2.Result.Content));

            var trackList = resultQ2.Result.Content.Split(',');

            //Third question: Duration of the poll
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync(null, false, new DiscordEmbedBuilder().WithDescription("**Question 3:** How long do you want the poll to last in days? (max 7)"));

            do
            {
                await ctx.Message.GetNextMessageAsync(m => int.TryParse(m.Content, out dayCount));
                // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            } while (dayCount > 7 && dayCount < 1);

            //Create the poll
            //Start by building the embed field, and add them to a list for easy ordering
            await ctx.TriggerTypingAsync();

            var sb = new StringBuilder();
            var trackEmojiPairs = new Dictionary<DiscordEmoji, string>();

            for (var i = 0; i < trackList.Length; i++)
            {
                var emoji = DiscordEmoji.FromName(ctx.Client, squareBoxes[i]);
                trackEmojiPairs.Add(emoji, trackList[i]);
                sb.Append($"{emoji}\t{trackList[i]}\n");
            }

            var fieldString = sb.ToString().Substring(0, sb.Length - 1);

            //Build and submit the poll
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
                    Text = "Poll end date"
                },
                Timestamp = DateTime.UtcNow.AddDays(dayCount)
            };

            pollEmbed.AddField("Options to vote on", fieldString);

            //Show them the details and ask if they want to start the poll
            InteractivityResult<DiscordMessage> resultFinal;
            await ctx.RespondAsync(null, false, pollEmbed);
            await ctx.Channel.SendMessageAsync(null, false,
                new DiscordEmbedBuilder().WithDescription("Here is how the poll will look. If you are happy, respond **yes**, otherwise respond " +
                                                          "**no** to cancel it."));
            do
            {
                resultFinal = await ctx.Message.GetNextMessageAsync(m => 
                    m.Content.ToLower().Equals("yes") || m.Content.ToLower().Equals("no"));
            } while (resultQ1.TimedOut || string.IsNullOrEmpty(resultFinal.Result.Content));

            if (resultFinal.Result.Content.ToLower().Equals("no")) return;

            //TIME TO SUBMIT THE POLL
            //Find the channel to send the poll in and send it
            var pollChannel = ctx.Guild.Channels[pollSettings[1]];
            await pollChannel.TriggerTypingAsync();
            var pollMessage = await pollChannel.SendMessageAsync(null, false, pollEmbed);

            //Collect the reactions over the time period
            var reactions = await pollMessage.CollectReactionsAsync(TimeSpan.FromDays(dayCount));

            //Once time is up, print the reactions
            await pollChannel.TriggerTypingAsync();
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

            pollResultsEmbed.AddField("Options that were voted on", fieldString);
            await pollChannel.SendMessageAsync(null, false, pollResultsEmbed);
        }
    }
}
