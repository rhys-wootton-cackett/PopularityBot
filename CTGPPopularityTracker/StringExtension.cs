using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTGPPopularityTracker
{
    public static class StringExtension
    {
        public static IEnumerable<string> Split(this string str, int iterateCount)
        {
            var words = new List<string>();

            for (var i = 0; i < str.Length; i += iterateCount)
                words.Add(str.Length - i >= iterateCount
                    ? str.Substring(i, iterateCount)
                    : str.Substring(i, str.Length - i));

            return words;
        }
    }
}
