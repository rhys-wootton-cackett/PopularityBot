using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace CTGPPopularityTracker
{
    public class Track
    {
        private double _wiimmfiScore;

        public string Name { get; set; }
        public string SHA1 { get; set; }
        public DateTime TrackAdded { get; set; }
        public int TimeTrialScore { get; set; }
        public int CompetitiveScore { get; set; }
        public double WiimmFiScore
        {
            get => _wiimmfiScore;
            set => _wiimmfiScore = CalculateWiimmFiScore(value);
        }
        public double Popularity => TimeTrialScore + WiimmFiScore;

        private double CalculateWiimmFiScore(double score)
        {
            double days = (DateTime.UtcNow - this.TrackAdded).Days <= 84 ? (DateTime.UtcNow - this.TrackAdded).Days : 84;
            return score * Math.Pow(0.5, (days / 7.0) / 4);
        }
    }
}
