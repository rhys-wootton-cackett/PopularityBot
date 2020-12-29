﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace CTGPPopularityTracker
{
    public class PopularityTracker
    {
        public Dictionary<(string, string), (int, int)> Tracks { get; }

        public DateTime LastUpdated { get; set; }
        public const string CtgpTtLink = "http://tt.chadsoft.co.uk/ctgp-leaderboards.json";
        public const string WiimmFiLink = "https://wiimmfi.de/stats/track/mv/ctgp?p=std,c0,0,";

        public PopularityTracker()
        {
            Tracks = new Dictionary<(string, string), (int, int)>();
        }

        /// <summary>
        /// Updates the popularity of tracks on Time Trials and WiimmFi usage.
        /// </summary>
        public async Task UpdatePopularity()
        {
            // Clear the Dictionary to allow for new values.
            Tracks.Clear();

            await GetTimeTrialPopularity();
            await GetWiimmFiPopularity();
            LastUpdated = DateTime.UtcNow;

            Console.WriteLine("Updated track list");
            
        }

        /// <summary>
        /// Gets the popularity from the CTGP Time Trial JSON API and adds them to
        /// the Tracks dictionary.
        /// </summary>
        private async Task GetTimeTrialPopularity()
        {
            //Connect to the API and put it in a JSON object
            Console.WriteLine("Grabbing latest CTGP TT data from API...");
            using var ctgpWc = new WebClient();
            var json = await ctgpWc.DownloadStringTaskAsync(new Uri(CtgpTtLink));
            dynamic jsonTt = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject(json));

            //Add the track and SHA1 of the track to the Tracks dictionary, and 
            //for every entry, increase its popularity accordingly.
            Console.WriteLine("Adding tracks to dictionary, please wait...");
            if (jsonTt != null)
                foreach (var trackJson in jsonTt.leaderboards)
                {
                    string trackName = trackJson.name;
                    string trackId = trackJson.trackId;

                    trackId = trackId.ToLower();

                    var track = (trackName, trackID: trackId);

                    if (!Tracks.ContainsKey(track))
                    {
                        Tracks.Add(track, (trackJson.popularity, 0));
                    }
                    else
                    {
                        var newPopularity = Tracks[track];
                        newPopularity.Item1 += (int) trackJson.popularity;
                        Tracks[track] = newPopularity;
                    }
                }
        }

        /// <summary>
        /// Gets the popularity from the top 400 tracks on the WiimmFi stats website
        /// and adds them to the current Tracks dictionary.
        /// </summary>
        /// <returns></returns>
        private async Task GetWiimmFiPopularity()
        {
            //Increment this counter by 100 each time.
            int startPoint = 0, tracksUpdated = 0;

            //Connect to the website and get the table whilst the start point is less
            //than or equal to 400
            using var wiimmWc = new WebClient();
            Console.WriteLine("Updating results from WiimmFi statistics, this may take a while...");

            while (tracksUpdated < 216)
            {
                //Extract the table and start looping through rows adding the track popularity count.
                var doc = new HtmlDocument();
                var newLink = WiimmFiLink + startPoint;
                doc.LoadHtml(await wiimmWc.DownloadStringTaskAsync(newLink));

                foreach (var row in doc.DocumentNode.SelectNodes("//*[@id=\"p0-tbody\"]/tr"))
                {
                    //We know that potentially the name of the track will just be it's SHA1 value,
                    //so try that first.
                    var cells = row.SelectNodes("td");

                    //If cells is null, then skip
                    if (cells == null) continue;

                    var trackName = cells[2].InnerText;

                    var trackPopularity = CalculateWiimmFiTrackPopularity(cells);

                    var trackKey = Tracks.Keys.FirstOrDefault(key =>
                        key.Item2 != null && (trackName.Contains(key.Item2)));

                    //If the SHA1 is already on the page, just use that
                    if (!(trackKey.Item1 == null && trackKey.Item2 == null))
                    {
                        var wiimmPopularityTrack = Tracks[trackKey];
                        wiimmPopularityTrack.Item2 = trackPopularity;
                        Tracks[trackKey] = wiimmPopularityTrack;

                        //Log the track that was updated
                        Console.WriteLine($"Updated track: {trackKey.Item1} (TT: {Tracks[trackKey].Item1}, WF: {Tracks[trackKey].Item2})");
                        tracksUpdated++;

                        continue;
                    }

                    //Now try accessing the page and getting it's SHA1 hashes from there
                    //If no page exists, assume it lost and just continue looping.
                    var doc2 = new HtmlDocument();
                    var trackUrl = cells[2].Descendants("a").FirstOrDefault()?.Attributes["href"].Value;

                    if (trackUrl == null) continue;
                    doc2.LoadHtml(await wiimmWc.DownloadStringTaskAsync(trackUrl));

                    //Loop through the table finding the SHA1 hashes
                    var trackPossibleSha1 = new List<string>();
                    foreach (var row2 in doc2.DocumentNode.SelectNodes("/html/body/div[5]/table/tr[position() > 1]"))
                    {
                        if (!row2.SelectNodes("td")[0].InnerText.Contains("SHA1")) continue;
                        var hashes = row2.SelectNodes("td")[1].InnerText.Split("<br>");
                        trackPossibleSha1.AddRange(hashes);
                    }

                    //Check the hashes like above, but only check against the second key
                    foreach (var hashKey in trackPossibleSha1.Select(hash => Tracks.Keys.FirstOrDefault(key =>
                            key.Item2 != null && hash.Contains(key.Item2)))
                        .Where(hashKey => hashKey.Item1 != null || hashKey.Item2 != null))
                    {
                        var wiimmPopularitySHA = Tracks[hashKey];
                        wiimmPopularitySHA.Item2 = trackPopularity;
                        Tracks[hashKey] = wiimmPopularitySHA;

                        //Log the track that was updated
                        Console.WriteLine($"Updated track: {hashKey.Item1} (TT: {Tracks[hashKey].Item1}, WF: {Tracks[hashKey].Item2})");
                        tracksUpdated++;
                    }
                }

                //Change the value to adjust URL
                startPoint += 100;
            }
        }

        /// <summary>
        /// Calculates popularity for WiimmFi statistics, which is determined using an
        /// exponentially decaying function based on the number of times a track has been
        /// played over a 3 week period.
        /// https://docs.google.com/document/d/1C8grliYKX-d5vtrzCJ8DM1oAyANC2sTTfOzBlJeMzaQ/edit?usp=sharing
        /// </summary>
        /// <param name="trackCells">The track cells from WiimmFi</param>
        /// <returns>The popularity of the track.</returns>
        private static int CalculateWiimmFiTrackPopularity(HtmlNodeCollection trackCells)
        {
            //Start by getting the date it was added
            var dateString = Regex.Replace(trackCells[8].Attributes["title"].Value, "UTC.*", "");
            var date = DateTime.Parse(dateString);

            //Work out how many weeks need to be accounted for
            long days = (DateTime.UtcNow - date).TotalDays <= 196 ? (DateTime.UtcNow - date).Days : 196;

            //Run the mathematical function
            if (trackCells[7].InnerText.Contains("–"))
            {
                return 0;
            }
            var popularity = double.Parse(trackCells[7].InnerText) * Math.Pow(0.5, days / 56.0);

            return (int)Math.Round(popularity);
        }

        /// <summary>
        /// https://stackoverflow.com/questions/20156/is-there-an-easy-way-to-create-ordinals-in-c
        /// </summary>
        /// <param name="num">Number to create ordinal for.</param>
        /// <returns></returns>
        private static string AddOrdinal(int num)
        {
            if (num <= 0) return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            return (num % 10) switch
            {
                1 => num + "st",
                2 => num + "nd",
                3 => num + "rd",
                _ => num + "th"
            };
        }

        /// <summary>
        /// Generate a string containing 'count' amount of tracks from 'startPoint' in ascending order.
        /// If 'reverse' is specified, it will generate a list in descending order. It can sort by overall
        /// popularity or by specifics.
        /// </summary>
        /// <param name="startPoint">The starting point for the list</param>
        /// <param name="count">The number of Tracks you want</param>
        /// <param name="reverse">True if wanting list in descending order</param>
        /// <param name="sortBy">How to sort the list according to a specific popularity value</param>
        /// <returns>A string with the tracks in order</returns>
        public string GetSortedListAsString(int startPoint, int count, bool reverse = false, string sortBy = null)
        {
            //Check that variables will work, and if not just reject the command
            if (startPoint > Tracks.Count) return null;

            // Sort all tracks by sortBy value
            var tracksSorted = SortTrackList(sortBy);

            //Put list into a string separated by new lines (two spaces)
            var sb = new StringBuilder();

            if (reverse)
            {
                for (var i = startPoint; i > startPoint - count; i--)
                {
                    //Format string according to sortBy
                    var trackLine = sortBy switch
                    {
                        "tt" => $"**{AddOrdinal(i)}:** {tracksSorted[i - 1].Key.Item1} ({tracksSorted[i - 1].Value.Item1})\n",
                        "wf" => $"**{AddOrdinal(i)}:** {tracksSorted[i - 1].Key.Item1} ({tracksSorted[i - 1].Value.Item2})\n",
                        _ =>
                            $"**{AddOrdinal(i)}:** {tracksSorted[i - 1].Key.Item1} ({tracksSorted[i - 1].Value.Item1 + tracksSorted[i - 1].Value.Item2}) `{tracksSorted[i - 1].Value.Item1} + {tracksSorted[i - 1].Value.Item2}`\n"
                    };

                    sb.Append(trackLine);
                }
            }
            else
            {
                for (var i = startPoint; i < startPoint + count; i++)
                {
                    //Format string according to sortBy
                    var trackLine = sortBy switch
                    {
                        "tt" => $"**{AddOrdinal(i + 1)}:** {tracksSorted[i].Key.Item1} ({tracksSorted[i].Value.Item1})\n",
                        "wf" => $"**{AddOrdinal(i + 1)}:** {tracksSorted[i].Key.Item1} ({tracksSorted[i].Value.Item2})\n",
                        _ =>
                            $"**{AddOrdinal(i + 1)}:** {tracksSorted[i].Key.Item1} ({tracksSorted[i].Value.Item1 + tracksSorted[i].Value.Item2}) `{tracksSorted[i].Value.Item1} + {tracksSorted[i].Value.Item2}`\n"
                    };

                    sb.Append(trackLine);
                }
            }

            return sb.ToString().Substring(0, sb.Length - 1);
        }

        /// <summary>
        /// Generate a string containing tracks that contain 'searchParam' in ascending order.
        /// </summary>
        /// <param name="searchParam">The search parameter</param>
        /// <param name="sortBy">How to sort the list according to a specific popularity value</param>
        /// <returns>A string with the tracks containing the search parameter in order</returns>
        public string FindTracksBasedOnParameter(string searchParam, string sortBy = null)
        {
            // Sort all tracks by sortBy value
            var tracksSorted = SortTrackList(sortBy);

            var sb = new StringBuilder();
            var count = 0;

            //Loop through the list
            for (var i = 0; i < Tracks.Count; i++)
            {
                var found = false;

                //If the search parameter is one word, look through words only.
                //If the search parameter is multiple words, search using contains
                if (searchParam.Split(" ").Length > 1)
                {
                    if (tracksSorted[i].Key.Item1.ToLower().Contains(searchParam.ToLower())) found = true;
                }
                else
                {
                    //Split track name into an array, and search equality of exact words.
                    var trackWords = tracksSorted[i].Key.Item1.Split(' ');

                    foreach (var word in trackWords)
                    {
                        if (!word.ToLower().Equals(searchParam.ToLower())) continue;
                        found = true;
                    }
                }

                //If not found, loop again
                if (!found) continue;

                //Get the sum of the popularity
                //Format string according to sortBy
                var trackLine = sortBy switch
                {
                    "tt" => $"**{AddOrdinal(i + 1)}:** {tracksSorted[i].Key.Item1} ({tracksSorted[i].Value.Item1})\n",
                    "wf" => $"**{AddOrdinal(i + 1)}:** {tracksSorted[i].Key.Item1} ({tracksSorted[i].Value.Item2})\n",
                    _ =>
                        $"**{AddOrdinal(i + 1)}:** {tracksSorted[i].Key.Item1} ({tracksSorted[i].Value.Item1 + tracksSorted[i].Value.Item2}) `{tracksSorted[i].Value.Item1} + {tracksSorted[i].Value.Item2}`\n"
                };

                sb.Append(trackLine);
                count++;
                if (count == 25) break;
            }

            if (count == 25) sb.Append("\n*Only showing the first 25 matches. Refine your search.*\n");
            return sb.Length == 0 ? "*No results found*" : sb.ToString().Substring(0, sb.Length - 1);
        }

        private List<KeyValuePair<(string, string), (int, int)>> SortTrackList(string sortBy)
        {
            var tracksSorted = Tracks.ToList();
            tracksSorted.Sort((pair1, pair2) =>
            {
                switch (sortBy)
                {
                    //If sortBy is null, use the sum
                    case "tt":
                        return pair2.Value.Item1.CompareTo(pair1.Value.Item1);
                    case "wf":
                        return pair2.Value.Item2.CompareTo(pair1.Value.Item2);
                    default:
                        var pair2Sum = pair2.Value.Item1 + pair2.Value.Item2;
                        var pair1Sum = pair1.Value.Item1 + pair1.Value.Item2;
                        return pair2Sum.CompareTo(pair1Sum);
                }
            });

            return tracksSorted;
        }
    }
}
