using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace CTGPPopularityTracker
{
    public class PopularityTracker
    {
        public List<Track> CtgpTracks { get; }
        public List<Track> NintendoTracks { get; }

        public DateTime LastUpdated { get; set; }

        private const string CtgpTtLink = "http://tt.chadsoft.co.uk/ctgp-leaderboards.json";
        private const string NintendoTtLink = "http://tt.chadsoft.co.uk/original-track-leaderboards.json";
        private const string CtgpWiimmFiLink = "https://wiimmfi.de/stats/track/wv/ctgp?p=std,c0,0,";
        private const string NintendoWiimmFiLink = "https://wiimmfi.de/stats/track/wv/all?p=std,c0,0";

        public PopularityTracker()
        {
            CtgpTracks = new List<Track>();
            NintendoTracks = new List<Track>();
        }

        /// <summary>
        /// Updates the popularity of tracks on Time Trials and WiimmFi usage.
        /// </summary>
        public async Task UpdatePopularity()
        {
            // Clear the Dictionaries to allow for new values.
            CtgpTracks.Clear();
            NintendoTracks.Clear();

            await GetTimeTrialPopularity(NintendoTtLink, NintendoTracks);
            await GetWiimmFiPopularity(NintendoWiimmFiLink, 32, NintendoTracks);
            await GetTimeTrialPopularity(CtgpTtLink, CtgpTracks);
            await GetWiimmFiPopularity(CtgpWiimmFiLink, 218, CtgpTracks);

            LastUpdated = DateTime.Now;

            Console.WriteLine("Updated track list");

        }

        /// <summary>
        /// Gets the popularity from the CTGP Time Trial JSON API and adds them to
        /// the Tracks dictionary.
        /// </summary>
        private static async Task GetTimeTrialPopularity(string link, List<Track> tracks)
        {
            //Connect to the API and put it in a JSON object
            Console.WriteLine("Grabbing latest TT data from API...");
            using var ctgpWc = new WebClient();
            var json = await ctgpWc.DownloadStringTaskAsync(new Uri(link));
            dynamic jsonTt = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject(json));

            //Add the track and SHA1 of the track to the Tracks dictionary, and 
            //for every entry, increase its popularity accordingly.
            Console.WriteLine("Adding tracks to dictionary, please wait...");
            if (jsonTt != null)
                foreach (var trackJson in jsonTt.leaderboards)
                {
                    var existingTrack = tracks.FindIndex(t => t.SHA1.Contains(trackJson.trackId.ToString()));

                    if (existingTrack < 0)
                    {
                        tracks.Add(new Track()
                        {
                            Name = trackJson.name, 
                            SHA1 = trackJson.trackId,
                            TimeTrialScore = trackJson.popularity
                        });
                    }
                    else
                    {
                        tracks[existingTrack].TimeTrialScore += int.Parse(trackJson.popularity.ToString());
                    }
                }
        }

        /// <summary>
        /// Gets the popularity from the top 400 tracks on the WiimmFi stats website
        /// and adds them to the current Tracks dictionary.
        /// </summary>
        /// <returns></returns>
        private static async Task GetWiimmFiPopularity(string link, int trackListSize, List<Track> tracks)
        {
            //Increment this counter by 100 each time.
            int startPoint = 0, tracksUpdated = 0;

            //Connect to the website and get the table whilst the start point is less
            //than or equal to 400
            using var wiimmWc = new WebClient();
            Console.WriteLine("Updating results from WiimmFi statistics, this may take a while...");

            while (true)
            {
                //Extract the table and start looping through rows adding the track popularity count.
                var doc = new HtmlDocument();
                var newLink = link + startPoint;
                doc.LoadHtml(await wiimmWc.DownloadStringTaskAsync(newLink));

                foreach (var row in doc.DocumentNode.SelectNodes("//*[@id=\"p0-tbody\"]/tr"))
                {
                    //We know that potentially the name of the track will just be it's SHA1 value,
                    //so try that first.
                    var cells = row.SelectNodes("td");

                    //If cells is null, then skip
                    if (cells == null) continue;

                    var trackShaOnPage = cells[2].InnerText;

                    if (trackShaOnPage.Contains("SHA1:"))
                    {
                        Console.WriteLine("Testing time!");
                    }

                    var trackIndex = tracks.FindIndex(t => trackShaOnPage.ToUpper().Contains(t.SHA1));

                    //If the SHA1 is already on the page, just use that
                    if (trackIndex >= 0)
                    {
                        tracks[trackIndex].TrackAdded = DateTime.Parse(Regex.Replace(cells[8].Attributes["title"].Value, "UTC.*", ""));
                        if (cells[7].InnerText.Contains("–")) tracks[trackIndex].WiimmFiScore = 0;
                        else tracks[trackIndex].WiimmFiScore = double.Parse(cells[7].InnerText);

                        //Log the track that was updated
                        Console.WriteLine($"Updated track: {tracks[trackIndex].Name} (TT: {tracks[trackIndex].TimeTrialScore}, WF: {tracks[trackIndex].WiimmFiScore})");
                        tracksUpdated++;

                        if (tracksUpdated == trackListSize) return;
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
                        var hashes = row2.SelectNodes("td")[1].InnerText.Split(40);
                        trackPossibleSha1.AddRange(hashes);
                    }

                    //Check the hashes like above, but only check against the second key
                    foreach (var hash in trackPossibleSha1)
                    {
                        var trackIndex2 = tracks.FindIndex(t => t.SHA1.Contains(hash.ToUpper()));
                        if (trackIndex2 >= 0)
                        {
                            tracks[trackIndex2].TrackAdded = DateTime.Parse(Regex.Replace(cells[8].Attributes["title"].Value, "UTC.*", ""));
                            if (cells[7].InnerText.Contains("–")) tracks[trackIndex2].WiimmFiScore = 0;
                            else tracks[trackIndex2].WiimmFiScore = double.Parse(cells[7].InnerText);

                            //Log the track that was updated
                            Console.WriteLine($"Updated track: {tracks[trackIndex2].Name} (TT: {tracks[trackIndex2].TimeTrialScore}, WF: {tracks[trackIndex2].WiimmFiScore})");
                            tracksUpdated++;

                            if (tracksUpdated == trackListSize) return;
                        }
                    }
                }

                //Change the value to adjust URL
                startPoint += 100;

                if (startPoint <= 200) continue;
                foreach (var track in tracks.Where(track => track.WiimmFiScore == 0))
                {
                    Console.WriteLine(track.Name);
                }
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

            return (num % 100) switch
            {
                11 => num + "th",
                12 => num + "th",
                13 => num + "th",
                _ => (num % 10) switch
                {
                    1 => num + "st",
                    2 => num + "nd",
                    3 => num + "rd",
                    _ => num + "th"
                }
            };
        }

        /// <summary>
        /// Generate a string containing 'count' amount of tracks from 'startPoint' in ascending order.
        /// If 'reverse' is specified, it will generate a list in descending order. It can sort by overall
        /// popularity or by specifics.
        /// </summary>
        /// <param name="tracks">The track list</param>
        /// <param name="startPoint">The starting point for the list</param>
        /// <param name="count">The number of Tracks you want</param>
        /// <param name="reverse">True if wanting list in descending order</param>
        /// <param name="sortBy">How to sort the list according to a specific popularity value</param>
        /// <returns>A string with the tracks in order</returns>
        public string GetSortedListAsString(List<Track> tracks, int startPoint, int count, bool reverse = false, string sortBy = null)
        {
            //Check that variables will work, and if not just reject the command
            if (startPoint > tracks.Count) return null;

            // Sort all tracks by sortBy value
            var tracksSorted = SortTrackList(tracks, sortBy);

            //Put list into a string separated by new lines (two spaces)
            var sb = new StringBuilder();

            for (var i = 0; i < count; i++)
            {
                //If reverse, start from the end of the list
                var placement = reverse ? tracks.Count - 1 - i : startPoint + i;

                //Format string according to sortBy
                var trackLine = sortBy switch
                {
                    "tt" => $"**{AddOrdinal(placement + 1)}:** {tracksSorted[placement].Name} ({tracksSorted[placement].TimeTrialScore})\n",
                    "wf" => $"**{AddOrdinal(placement + 1)}:** {tracksSorted[placement].Name} ({(int)(tracksSorted[placement].WiimmFiScore)})\n",
                    _ =>
                        $"**{AddOrdinal(placement + 1)}:** {tracksSorted[placement].Name} ({(int)(tracksSorted[placement].TimeTrialScore + tracksSorted[placement].WiimmFiScore)})\n"
                };

                sb.Append(trackLine);
            }

            return sb.ToString().Substring(0, sb.Length - 1);
        }

        /// <summary>
        /// Generate a string containing tracks that contain 'searchParam' in ascending order.
        /// </summary>
        /// <param name="tracks">The track list</param>
        /// <param name="searchParam">The search parameter</param>
        /// <param name="sortBy">How to sort the list according to a specific popularity value</param>
        /// <returns>A string with the tracks containing the search parameter in order</returns>
        public string FindTracksBasedOnParameter(List<Track> tracks, string searchParam, string sortBy = null)
        {
            // Sort all tracks by sortBy value
            var tracksSorted = SortTrackList(tracks, sortBy);

            var sb = new StringBuilder();
            var count = 0;

            //Loop through the list
            for (var i = 0; i < tracks.Count; i++)
            {
                var found = false;

                //If the search parameter is 3 characters or less, look through words only.
                //Otherwise, search using contains
                if (searchParam.Length <= 3)
                {
                    //Split track name into an array, and search equality of exact words.
                    var trackWords = tracksSorted[i].Name.Split(' ');

                    foreach (var word in trackWords)
                    {
                        if (!word.ToLower().Equals(searchParam.ToLower())) continue;
                        found = true;
                    }
                }
                else
                {
                    if (tracksSorted[i].Name.ToLower().Contains(searchParam.ToLower())) found = true;
                }

                //If not found, loop again
                if (!found) continue;

                //Format string according to sortBy
                var trackLine = sortBy switch
                {
                    "tt" => $"**{AddOrdinal(i + 1)}:** {tracksSorted[i].Name} ({tracksSorted[i].TimeTrialScore})\n",
                    "wf" => $"**{AddOrdinal(i + 1)}:** {tracksSorted[i].Name} ({(int)(tracksSorted[i].WiimmFiScore)})\n",
                    _ =>
                        $"**{AddOrdinal(i + 1)}:** {tracksSorted[i].Name} ({(int)(tracksSorted[i].TimeTrialScore + tracksSorted[i].WiimmFiScore)})\n"
                };

                sb.Append(trackLine);
                count++;
                if (count == 25) break;
            }

            if (count == 25) sb.Append("\n*Only showing the first 25 matches. Refine your search.*\n");
            return sb.Length == 0 ? "*No results found*" : sb.ToString().Substring(0, sb.Length - 1);
        }

        private static List<Track> SortTrackList(List<Track> tracks, string sortBy)
        {
            tracks = new List<Track>(sortBy switch
            {
                "wf" => tracks.OrderByDescending(t => t.WiimmFiScore),
                "tt" => tracks.OrderByDescending(t => t.TimeTrialScore),
                _ => tracks.OrderByDescending(t => t.Popularity)
            });

            return tracks;
        }
    }
}
