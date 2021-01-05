using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace CTGPPopularityTracker.Commands
{
    public class CommandResponseBuilder
    {
        private readonly DiscordEmbedBuilder _embedBaseTemplate;
        private readonly DiscordEmbedBuilder _embedListTemplate;

        public CommandResponseBuilder()
        {
            _embedBaseTemplate = new DiscordEmbedBuilder
            {
                Color = new DiscordColor("#FE0002")
            };

            _embedListTemplate = new DiscordEmbedBuilder
            {
                Color = new DiscordColor("#FE0002"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"Last updated"
                }
            };
        }

        /// <summary>
        /// Creates an embed for the specific rank command issued to it from the command handler.
        /// </summary>
        /// <param name="type">The type of ranking.</param>
        /// <param name="dictionary">The track dictionary.</param>
        /// <param name="sortby">What to sort the ranking by</param>
        /// <returns></returns>
        public DiscordEmbed CreateRankedEmbed(RankType type, IDictionary<(string, string), (int, int)> dictionary,
            string sortby)
        {
            var embed = _embedListTemplate.ClearFields().WithTimestamp(Program.Tracker.LastUpdated);

            //Create the fields depending on rank type.
            if (type != RankType.Bottom)
            {
                var top = Program.Tracker.GetSortedListAsString(dictionary, 0, 10, false, sortby);

                var fieldTitle = dictionary.Count == 32 ? "Nintendo - " : "CTGP - ";
                fieldTitle += sortby switch
                {
                    "tt" => "Top 10 (Time Trial)",
                    "wf" => "Top 10 (WiimmFi)",
                    _ => "Top 10"
                };

                embed.AddField(fieldTitle, top, true);
            }

            if (type != RankType.Top)
            {
                var bottom = Program.Tracker.GetSortedListAsString(dictionary, dictionary.Count, 10, true, sortby);

                var fieldTitle = dictionary.Count == 32 ? "Nintendo - " : "CTGP - ";
                fieldTitle += sortby switch
                {
                    "tt" => "Bottom 10 (Time Trial)",
                    "wf" => "Bottom 10 (WiimmFi)",
                    _ => "Bottom 10"
                };

                embed.AddField(fieldTitle, bottom, true);
            }

            return embed;
        }

        public DiscordEmbed CreateRangedEmbed(IDictionary<(string, string), (int, int)> dictionary, int start, int end,
            string sortby)
        {
            var embed = _embedListTemplate.ClearFields().WithTimestamp(Program.Tracker.LastUpdated);

            if (end - start < 1) return _embedBaseTemplate.WithDescription(
                    "*Please adjust your end point. It has to be greater than your start point.*");

            if (end - start > 24) return _embedBaseTemplate.WithDescription(
                "*Please adjust your end point. I can only list 25 tracks at a time.*");

            if (start < 1) return _embedBaseTemplate.WithDescription(
                    "*Please adjust your start point. It has to be greater than or equal to 1.*");

            if (start > dictionary.Count) return _embedBaseTemplate.WithDescription(
                $"*Please adjust your start point. It has to be less than the number of tracks available ({dictionary.Count})*");

            var tracks = start + (end - start) > dictionary.Count ? 
                Program.Tracker.GetSortedListAsString(dictionary, start - 1, dictionary.Count - start + 1, false, sortby) : 
                Program.Tracker.GetSortedListAsString(dictionary, start - 1, end - start + 1, false, sortby);

            var fieldTitle = dictionary.Count == 32 ? "Nintendo - " : "CTGP - ";
            fieldTitle += sortby switch
            {
                "tt" => "Custom range (Time Trial)",
                "wf" => "Custom range (WiimmFi)",
                _ => "Custom range"
            };

            embed.AddField(fieldTitle, tracks);
            return embed;
        }

        public DiscordEmbed CreateSearchEmbed(IDictionary<(string, string), (int, int)> dictionary, string search)
        {
            var embed = _embedListTemplate.ClearFields().WithTimestamp(Program.Tracker.LastUpdated);

            var paramInput = search.Split(" ");
            
            //Get tracks in order based on popularity
            var tracks = paramInput[^1] switch
            {
                "tt" => Program.Tracker.FindTracksBasedOnParameter(dictionary, search.Substring(0, search.Length - 3), "tt"),
                "wf" => Program.Tracker.FindTracksBasedOnParameter(dictionary, search.Substring(0, search.Length - 3), "wf"),
                _ => Program.Tracker.FindTracksBasedOnParameter(dictionary, search)
            };

            var fieldTitle = paramInput[^1] switch
            {
                "tt" => $"Tracks containing \"{search.Substring(0, search.Length - 3)}\" (Time Trial only)",
                "wf" => $"Tracks containing \"{search.Substring(0, search.Length - 3)}\" (WiimmFi only)",
                _ => $"Tracks containing \"{search}\""
            };

            embed.AddField(fieldTitle, tracks);
            return embed;
        }
    }
}
