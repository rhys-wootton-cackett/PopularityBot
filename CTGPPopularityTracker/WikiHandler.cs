using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CTGPPopularityTracker
{
    public class WikiHandler
    {
        public HashSet<string> WikiList { get; }

        public WikiHandler()
        {
            WikiList = new HashSet<string>();
        }

        /// <summary>
        /// Gets all the custom tracks that are on the Custom Mario Kart Wiiki.
        /// </summary>
        public async Task GetWikiTrackList()
        {
            var categoryList = new string[] {"Track/Custom", "Track/Import", "Track/Retro"};
            using var wikiWC = new WebClient();
            var nextJSON = "";

            Console.WriteLine("Grabbing latest wiki entries from API...");

            //We need to enumerate through 3 different categories on the wiki site,
            //which can be achieved using the JSON API provided.
            foreach (var category in categoryList)
            {
                do
                {
                    //Connect to the API and put it in a JSON object
                    var link =
                        $"http://wiki.tockdom.com/w/api.php?action=query&list=categorymembers&cmtitle=Category:{category}&cmlimit=500&format=json&cmcontinue={nextJSON}";
                    var json = await wikiWC.DownloadStringTaskAsync(new Uri(link));

                    //Change since C# cannot use names with hyphens
                    json = json.Replace("query-continue", "querycontinue");

                    dynamic jsonWiki = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter()));

                    if (jsonWiki == null) continue;

                    //Add each item to the tracklist
                    foreach (var trackJson in jsonWiki.query.categorymembers)
                    {
                        WikiList.Add(trackJson.title.ToString());
                    }

                    //Get the next page for the API. If it doesn't exist, break out.
                    nextJSON = json.Contains("querycontinue") ? 
                        (string) jsonWiki.querycontinue.categorymembers.cmcontinue : null;
                } while (nextJSON != null);
            }
        }

        /// <summary>
        /// Searches the WikiList for tracks that contain the search parameter.
        /// </summary>
        /// <param name="searchParam">The parameter to search for.</param>
        /// <returns>A string array containing tracks containing the search parameter.</returns>
        public string[] FindWikiTracks(string searchParam)
        {
            var foundTracks = new List<string>();

            foreach (var track in WikiList)
            {
                //If the search parameter is 3 characters or less, look through words only.
                //Otherwise, search using contains
                if (searchParam.Length <= 3)
                {
                    //Split track name into an array, and search equality of exact words.
                    var trackWords = track.Split(' ');

                    foundTracks.AddRange(from word in trackWords where word.ToLower().Equals(searchParam.ToLower()) select track);
                }
                else
                {
                    if (track.ToLower().Contains(searchParam.ToLower())) foundTracks.Add(track);
                }
            }

            return foundTracks.ToArray();
        }

        /// <summary>
        /// Creates a dictionary of all the information related to a track from it's wiki page.
        /// </summary>
        /// <param name="track">The track name</param>
        /// <returns>A dictionary with all the details of a track from the wiki.</returns>
        public async Task<Dictionary<string, string>> GetWikiTrackAsync(string track)
        {
            var trackDict = new Dictionary<string, string>();
            var link = $"http://wiki.tockdom.com/wiki/{track.Replace(" ", "_")}";
            trackDict.Add("Url", link);

            //Grab the track page
            using var wikiPage = new WebClient();
            var doc = new HtmlDocument();
            doc.LoadHtml(await wikiPage.DownloadStringTaskAsync(link));
            
            //Find the table with the correct information
            var tables = doc.DocumentNode.SelectNodes("//table");

            //Get table information first
            trackDict.Add("Name", tables[0].SelectSingleNode("//caption").InnerText.Replace("\n", ""));

            foreach (var row in tables[0].SelectNodes("tr"))
            {
                //If it is about editors, skip.
                if (row.ChildNodes[1].InnerText.Contains("Editors")) continue;

                //If it is not a WBZ files or download, add to the dictionary.
                if (!(row.ChildNodes[1].InnerText.Contains("WBZ files") ||
                      row.ChildNodes[1].InnerText.Contains("Download")))
                {
                    trackDict.Add(row.ChildNodes[1].InnerText.Replace(":", "").Replace("\n", ""), 
                        row.ChildNodes[3].InnerText.Replace("\n", ""));
                    continue;
                }

                //If link doesn't exist, just discard
                if (row.ChildNodes[3].Descendants("a").FirstOrDefault()?.Attributes["href"] == null) continue;

                //There may be multiple links. If so, split into a comma list.
                if (row.ChildNodes[3].Descendants("ul").Any())
                {
                    var links = new List<string>();

                    foreach (var node in row.ChildNodes[3].SelectNodes("ul/li"))
                    {
                        var mdLink = row.ChildNodes[3].Descendants("a").FirstOrDefault()?.Attributes["href"].Value
                            .Replace("\n", "");
                        links.Add($"[{node.InnerText.Replace("\n", "")}]({mdLink})");
                    }

                    trackDict.Add(row.ChildNodes[1].InnerText.Replace(":", "").Replace("\n", ""), 
                        string.Join(", ", links));
                }
                else
                {
                    var mdLink = row.ChildNodes[3].Descendants("a").FirstOrDefault()?.Attributes["href"].Value
                        .Replace("\n", "");
                    trackDict.Add(row.ChildNodes[1].InnerText.Replace(":", "").Replace("\n", ""),
                        $"[{row.ChildNodes[3].InnerText.Replace("\n", "")}]({mdLink})");
                }
            }

            //Get the description
            trackDict.Add("Description", doc.DocumentNode.SelectSingleNode("//*[@id=\"mw-content-text\"]/p[1]").InnerText.Replace("\n", ""));

            //Decode all items
            foreach (var (key, _) in trackDict)
            {
                trackDict[key] = WebUtility.HtmlDecode(trackDict[key]);
            }

            return trackDict;
        }
    }
}
