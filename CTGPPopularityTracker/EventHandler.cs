using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;

namespace CTGPPopularityTracker
{
    public class EventHandler
    {
        public async Task<Task> OnCommandError(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            switch (e.Exception)
            {
                case CommandNotFoundException _:
                    return Task.CompletedTask;
                case ChecksFailedException _:
                    {
                        foreach (var attr in ((ChecksFailedException)e.Exception).FailedChecks)
                        {
                            if (attr is CooldownAttribute attribute)
                            {
                                var cooldown = attribute;
                                var secondsString = cooldown.GetRemainingCooldown(e.Context).Seconds > 1 ? 
                                    $"{cooldown.GetRemainingCooldown(e.Context).Seconds} seconds" : 
                                    "1 second";
                                var m = e.Context?.Channel?.SendMessageAsync(
                                    $"{e.Context.User.Mention}, you can use this command again in {secondsString}.");
                                await Task.Delay(TimeSpan.FromSeconds(10));
                                if (m != null) await m.Result.DeleteAsync();
                            }
                            else
                            {
                                var m = e.Context?.Channel?.SendMessageAsync($"{e.Context.User.Mention}{ParseFailedCheck(attr)}");
                                await Task.Delay(TimeSpan.FromSeconds(10));
                                if (m != null) await m.Result.DeleteAsync();
                            }
                        }

                        return Task.CompletedTask;
                    }

                default:
                    {
                        DiscordEmbed error = new DiscordEmbedBuilder
                        {
                            Color = new DiscordColor("#FE0002"),
                            Description = "An internal server error has occurred. I am probably trying to grab the popularity for the first time, " +
                                          "so please wait a few minutes and then try again. If it still doesn't work, report it to the developer."
                        };
                        var m = e.Context?.Channel?.SendMessageAsync("", false, error);
                        await Task.Delay(TimeSpan.FromSeconds(15));
                        if (m != null) await m.Result.DeleteAsync();
                        return Task.CompletedTask;
                    }
            }
        }

        private static string ParseFailedCheck(CheckBaseAttribute attr)
        {
            return attr switch
            {
                RequireOwnerAttribute _ => ", only the server owner can use that command.",
                RequirePermissionsAttribute _ => ", you don't have permission to do that.",
                RequireRolesAttribute _ => ", you do not have a required role.",
                RequireUserPermissionsAttribute _ => ", you don't have permission to do that.",
                RequireNsfwAttribute _ => ", this command can only be used in an NSFW channel.",
                RequirePollStartChannelAttribute => ", you cannot start a poll in this channel.",
                _ => ", an unknown Discord API error has occurred, contact the developer and please try again later."
            };
        }
    }
}
