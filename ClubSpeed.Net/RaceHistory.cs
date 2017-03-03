using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClubSpeed.Net
{
    public class RaceHistory
    {
        public string RacerName;
        public int CustomerId;
        public List<RaceResult> Races;

        public RaceHistory()
        {
            Races = new List<RaceResult>();
        }
    }
}
