using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;

namespace ClubSpeed.Net
{
    public static class ClubSpeedAustin
    {
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
                raceHistory.Races.Add(raceResult);
            }

            return raceHistory;
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
        public List<RaceResult> Races;

        public RaceHistory()
        {
            Races = new List<RaceResult>();
        }
    }
}
