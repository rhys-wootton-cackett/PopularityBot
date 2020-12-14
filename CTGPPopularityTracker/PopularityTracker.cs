using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace CTGPPopularityTracker
{
    public class PopularityTracker
    {
        public Dictionary<(string, string), int> Tracks { get; }

        public DateTime LastUpdated { get; set; }
        public const string CtgpTtLink = "http://tt.chadsoft.co.uk/ctgp-leaderboards.json";
        public const string WiimmFiLink = "https://wiimmfi.de/stats/track/wv/ctgp?p=std,c0,0,";

        public PopularityTracker()
        {
            Tracks = new Dictionary<(string, string), int>();
        }

        /// <summary>
        /// Updates the popularity of tracks on Time Trials and WiimmFi usage.
        /// </summary>
        public async Task UpdatePopularity()
        {
            // Clear the Dictionary to allow for new values.
            Tracks.Clear();
            LastUpdated = DateTime.UtcNow;

            await GetTimeTrialPopularity();
            await GetWiimmFiPopularity();

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
            foreach (var trackJson in jsonTt.leaderboards)
            {
                string trackName = trackJson.name;
                string trackId = trackJson.trackId;

                trackId = trackId.ToLower();

                var track = (trackName, trackID: trackId);

                if (!Tracks.ContainsKey(track))
                {
                    Tracks.Add(track, (int)trackJson.popularity);
                }
                else
                {
                    Tracks[track] += (int)trackJson.popularity;
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
            var startPoint = 0;

            //Connect to the website and get the table whilst the start point is less
            //than or equal to 400
            using var wiimmWc = new WebClient();
            Console.WriteLine("Updating results from WiimmFi statistics, this may take a while...");

            while (startPoint <= 400)
            {
                //Extract the table and start looping through rows adding the track popularity count.
                var doc = new HtmlDocument();
                var newLink = WiimmFiLink + startPoint;
                doc.LoadHtml(await wiimmWc.DownloadStringTaskAsync(newLink));

                foreach (var row in doc.DocumentNode.SelectNodes("//*[@id=\"p0-tbody\"]/tr"))
                {
                    //We know the name of the track is the third td, and the popularity is the 4th.
                    var cells = row.SelectNodes("td");

                    //If cells is null, then skip
                    if (cells == null) continue;

                    var trackName = cells[2].InnerText;

                    //If the track has been played 0 times, we can finish scanning since 
                    //the popularity of them will not change.
                    if (cells[3].InnerText.Contains("–")) return;
                    var trackPopularity = int.Parse(cells[3].InnerText);

                    var trackKey = Tracks.Keys.FirstOrDefault(key =>
                        key.Item2 != null && (trackName.Contains(key.Item1 ?? string.Empty) ||
                                              trackName.Contains(key.Item2)));

                    //If found using the track name, then adjust the value and restart loop
                    if (!(trackKey.Item1 == null && trackKey.Item2 == null))
                    {
                        Tracks[trackKey] += trackPopularity;
                        continue;
                    }

                    //The name may be different, so compare against the hashes on the page instead
                    //Start by going to the page containing the custom track
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
                        Tracks[hashKey] += trackPopularity;
                    }
                }

                //Change the value to adjust URL
                startPoint += 100;
            }
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
        /// If 'reverse' is specified, it will generate a list in descending order.
        /// </summary>
        /// <param name="startPoint">The starting point for the list</param>
        /// <param name="count">The number of Tracks you want</param>
        /// <param name="reverse">True if wanting list in descending order</param>
        /// <returns>A string with the tracks in order</returns>
        public string GetSortedListAsString(int startPoint, int count, bool reverse = false)
        {
            //Check that variables will work, and if not just reject the command
            if (startPoint > Tracks.Count) return null;

            // Sort all tracks
            var tracksSorted = Tracks.ToList();
            tracksSorted.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            //Put list into a string separated by new lines (two spaces)
            var sb = new StringBuilder();

            if (reverse)
            {
                for (var i = startPoint; i > startPoint - count; i--)
                {
                    sb.Append($"**{AddOrdinal(i)}:** {tracksSorted[i - 1].Key.Item1} ({tracksSorted[i - 1].Value})\n");
                }
            }
            else
            {
                for (var i = startPoint; i < startPoint + count; i++)
                {
                    sb.Append($"**{AddOrdinal(i + 1)}:** {tracksSorted[i].Key.Item1} ({tracksSorted[i].Value})\n");
                }
            }

            return sb.ToString().Substring(0, sb.Length - 1);
        }

        /// <summary>
        /// Generate a string containing tracks that contain 'searchParam' in ascending order.
        /// </summary>
        /// <param name="searchParam">The search parameter</param>
        /// <returns>A string with the tracks containing the search parameter in order</returns>
        public string FindTracksBasedOnParameter(string searchParam)
        {
            //Sort the tracks
            var tracksSorted = Tracks.ToList();
            tracksSorted.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            var sb = new StringBuilder();
            var count = 0;

            for (var i = 0; i < Tracks.Count; i++)
            {
                if (!tracksSorted[i].Key.Item1.ToLower().Contains(searchParam.ToLower())) continue;
                sb.Append($"**{AddOrdinal(i + 1)}:** {tracksSorted[i].Key.Item1} ({tracksSorted[i].Value})\n");
                count++;

                if (count == 25) break;
            }

            if (count == 25) sb.Append("\n*Only showing the first 25 matches. Refine your search.*\n");
            return sb.Length == 0 ? "*No results found*" : sb.ToString().Substring(0, sb.Length - 1);
        }
    }
}
