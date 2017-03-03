using System.Collections.Generic;

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
