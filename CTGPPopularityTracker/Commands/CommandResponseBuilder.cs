using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace CTGPPopularityTracker.Commands
{
    public class CommandResponseBuilder
    {
        public DiscordEmbedBuilder EmbedBaseTemplate { get; }
        private DiscordEmbedBuilder _embedListTemplate, _embedWikiTemplate;

        public CommandResponseBuilder()
        {
            EmbedBaseTemplate = new DiscordEmbedBuilder
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

            _embedWikiTemplate = new DiscordEmbedBuilder()
            {
                Color = new DiscordColor("#FE0002"),
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                {
                    Url = "http://wiki.tockdom.com/w/skins/common/images/ct-wiki.png"
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

        /// <summary>
        /// Creates an embed for the specific ranged command issued to it from the command handler.
        /// </summary>
        /// <param name="dictionary">The track dictionary</param>
        /// <param name="start">The start point</param>
        /// <param name="end">The end point</param>
        /// <param name="sortby">What to sort the ranking by</param>
        /// <returns></returns>
        public DiscordEmbed CreateRangedEmbed(IDictionary<(string, string), (int, int)> dictionary, int start, int end,
            string sortby)
        {
            var embed = _embedListTemplate.ClearFields().WithTimestamp(Program.Tracker.LastUpdated);

            if (end - start < 1) return EmbedBaseTemplate.WithDescription(
                    "*Please adjust your end point. It has to be greater than your start point.*");

            if (end - start > 24) return EmbedBaseTemplate.WithDescription(
                "*Please adjust your end point. I can only list 25 tracks at a time.*");

            if (start < 1) return EmbedBaseTemplate.WithDescription(
                    "*Please adjust your start point. It has to be greater than or equal to 1.*");

            if (start > dictionary.Count) return EmbedBaseTemplate.WithDescription(
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

        /// <summary>
        /// Creates an embed for the specific search command issued to it from the command handler.
        /// </summary>
        /// <param name="dictionary">The track dictionary</param>
        /// <param name="search">The search parameter</param>
        /// <returns></returns>
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

            var fieldTitle = dictionary.Count switch
            {
                32 => "Nintendo - ",
                218 => "CTGP - ",
                _ => ""
            };

            fieldTitle += paramInput[^1] switch
            {
                "tt" => $"Tracks containing \"{search.Substring(0, search.Length - 3)}\" (Time Trial only)",
                "wf" => $"Tracks containing \"{search.Substring(0, search.Length - 3)}\" (WiimmFi only)",
                _ => $"Tracks containing \"{search}\""
            };

            embed.AddField(fieldTitle, tracks);
            return embed;
        }

        public DiscordEmbed CreateWikiSelectEmbed(string[] trackList, string search)
        {
            var sb = new StringBuilder();
            var embed = EmbedBaseTemplate.ClearFields().WithDescription(
                "I have found more than one track that fits your criteria. Please respond with the number corresponding to the track you wish to see. If you don't wish to see any, respond with **0**.");

            for (var i = 0; i < trackList.Length && i < 15; i++)
            {
                sb.Append($"**{i + 1}:** {trackList[i]}\n");
            }

            if (trackList.Length > 15) sb.Append("\n*Only showing the first 25 matches. Refine your search.*");

            embed.AddField($"Wiki tracks containing {search}", sb.ToString());

            return embed;
        }

        public DiscordEmbed CreateWikiTrackEmbed(Dictionary<string, string> track)
        {
            var embed = _embedWikiTemplate.ClearFields().WithTitle(track["Name"]).WithDescription(track["Description"]).WithUrl(new Uri(track["Url"]));

            foreach (var (key, value) in track.Where(kvp => 
                !kvp.Key.Equals("Name") && !kvp.Key.Equals("Url") && !kvp.Key.Equals("Description")))
            {
                embed.AddField(key, value, true);
            }

            return embed;
        }
    }
}
