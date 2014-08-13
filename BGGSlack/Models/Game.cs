using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BGGSlack.Models
{
    public class Game
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Year { get; set; }
        public string BGRankName { get; set; }
        public string BGRankNum { get; set; }
        public string BGFamilyName { get; set; }
        public string BGFamilyNum { get; set; }
        public string URL { get; set; }
        public int UsersRated { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1}) {2}: {3} {4}: {5} {6}", Name, Year, BGRankName, BGRankNum, BGFamilyName, BGFamilyNum, URL);
        }

    }
}
