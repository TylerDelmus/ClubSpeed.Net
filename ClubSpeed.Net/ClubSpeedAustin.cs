using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;

namespace ClubSpeed.Net
{
    public static class ClubSpeedAustin
    {
        private static Dictionary<int, RaceHistory> _raceHistoryCache = new Dictionary<int, RaceHistory>();

        public static List<int> GetRacerIdsForHeat(int heatNo)
        {
            string url = string.Format("https://k1austin.clubspeedtiming.com/sp_center/HeatDetails.aspx?HeatNo={0}", heatNo);
            string html = new WebClient().DownloadString(url);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<int> custIds = new List<int>();

            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//a[contains(@href, 'RacerHistory.aspx?CustID=')]"))
                custIds.Add(int.Parse(node.Attributes["href"].Value.Split('=')[1]));

            return custIds;
        }

        public static RaceHistory GetRaceHistory(int custId)
        {
            string url = string.Format("https://k1austin.clubspeedtiming.com/sp_center/RacerHistory.aspx?CustID={0}", custId);
            string html = new WebClient().DownloadString(url);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            RaceHistory raceHistory = new RaceHistory();
            raceHistory.RacerName = doc.DocumentNode.SelectSingleNode("//span[@id='lblRacerName']").InnerText;
            raceHistory.CustomerId = custId;

            HtmlNodeCollection races = doc.DocumentNode.SelectNodes("//tr[@class='Normal']");

            foreach (HtmlNode node in races)
            {
                RaceResult raceResult;
                raceResult.BestLap = float.Parse(node.ChildNodes[4].InnerText);
                raceResult.HeatNo = int.Parse(node.ChildNodes[1].FirstChild.Attributes["href"].Value.Split('=')[1]);
                raceResult.Kart = int.Parse(node.ChildNodes[1].FirstChild.InnerText.Split(' ').Last());
                raceResult.Time = DateTime.Parse(node.ChildNodes[2].InnerText);

                //In very rare cases someone will have multiple karts in a single race. (See: custId 10167423 - Heat 123969)
                if (raceHistory.Races.ContainsKey(raceResult.HeatNo))
                    continue;

                raceHistory.Races.Add(raceResult.HeatNo, raceResult);
            }

            return raceHistory;
        }

        public static HeatResult GetHeatResults(int heatNo)
        {
            string url = string.Format("https://k1austin.clubspeedtiming.com/sp_center/HeatDetails.aspx?HeatNo={0}", heatNo);
            string html = new WebClient().DownloadString(url);

            //Check if we got a server error.
            if (html.Contains("System cannot process your request at this time."))
                return null;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            HeatResult result = new HeatResult();
            result.HeatNo = heatNo;
            result.DateTime = Convert.ToDateTime(doc.DocumentNode.SelectSingleNode("//span[@id='lblDate']").InnerText);

            try
            {
                foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//a[contains(@href, 'RacerHistory.aspx?CustID=')]"))
                {
                    int custId = int.Parse(node.Attributes["href"].Value.Split('=')[1]);
                    string racerName = node.InnerText;

                    //TODO: Identify duplicates by referencing kart number.
                    //For now we'll just ignore duplicate racers as they likely aren't important.
                    if (result.Racers.Exists( x => x.RacerName == racerName))
                        continue;

                    //Check if the customer is in the race history cache, if they aren't we will add them.
                    if (!_raceHistoryCache.ContainsKey(custId))
                        _raceHistoryCache.Add(custId, GetRaceHistory(custId));

                    //Check if we have the current heat number cached in the customer's race history. If not, update the race history cache.
                    if (!_raceHistoryCache[custId].Races.ContainsKey(heatNo))
                        _raceHistoryCache[custId] = GetRaceHistory(custId);

                    RaceResult race = _raceHistoryCache[custId].Races[heatNo];

                    result.Racers.Add(new Participant(custId, racerName, race.Kart));
                }
            }
            catch { return null; }

            foreach (HtmlNode lapTimesNode in doc.DocumentNode.SelectNodes("//table[@class='LapTimes']"))
            {
                string lapTimesOwner = lapTimesNode.FirstChild.FirstChild.InnerText;
                Participant racer = result.Racers.FirstOrDefault(x => x.RacerName == lapTimesOwner);

                //Ignore one of the duplicate named racer's laps as we cannot verify which is which.
                if (racer == null)
                    continue;

                foreach(HtmlNode lapTimeNode in lapTimesNode.ChildNodes[1].ChildNodes)
                {
                    //Ignore penalty count.
                    if (lapTimeNode == lapTimesNode.ChildNodes[1].FirstChild)
                        continue;

                    //Ignore blank laps(They got lapped)
                    if (lapTimeNode.ChildNodes[1].InnerText == "&nbsp;")
                        continue;

                    Lap lap;
                    lap.CustId = racer.CustId;
                    lap.Kart = racer.Kart;
                    lap.LapNum = int.Parse(lapTimeNode.FirstChild.InnerText);
                    lap.LapTime = float.Parse(lapTimeNode.ChildNodes[1].InnerText.Split(' ')[0]);
                    result.Laps.Add(lap);
                }
            }

            return result;
        }

        public static int GetKartNo(int custId, int heatNo)
        {
            string url = string.Format("https://k1austin.clubspeedtiming.com/sp_center/RacerHistory.aspx?CustID={0}", custId);
            string html = new WebClient().DownloadString(url);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlNode node = doc.DocumentNode.SelectSingleNode(string.Format("//a[contains(@href, 'HeatDetails.aspx?HeatNo={0}')]", heatNo));
            return int.Parse(node.InnerText.Split(' ').Last());
        }
    }

    public class HeatResult
    {
        public int HeatNo;
        public DateTime DateTime;
        public List<Participant> Racers;
        public List<Lap> Laps;

        public HeatResult()
        {
            Racers = new List<Participant>();
            Laps = new List<Lap>();
        }
    }

    public struct Lap
    {
        public int CustId;
        public int Kart;
        public int LapNum;
        public float LapTime;
    }

    public class Participant
    {
        public int CustId;
        public int Kart;
        public string RacerName;

        public Participant(int custId, string racerName, int kart)
        {
            CustId = custId;
            RacerName = racerName;
            Kart = kart;
        }
    }

    public struct RaceResult
    {
        public DateTime Time;
        public int HeatNo;
        public float BestLap;
        public int Kart;
    }

    public class RaceHistory
    {
        public string RacerName;
        public int CustomerId;
        public Dictionary<int, RaceResult> Races;

        public RaceHistory()
        {
            Races = new Dictionary<int, RaceResult>();
        }
    }
}
