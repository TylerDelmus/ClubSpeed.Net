using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Data;
using System.Data.SQLite;
using ClubSpeed.Net;

namespace Test
{
    public class Program
    {
        private static SQLiteDatabase _database;
        private static List<int> _observedHeatNos = new List<int>();

        private static void Main(string[] args)
        {
            //This assumes a SQLite database with the following tables exists during these tests.
            //CREATE TABLE 'Racers'(Id Integer, RacerName Text, UNIQUE(Id) ON CONFLICT REPLACE);
            //CREATE TABLE 'Races'(HeatNo Integer, CustId Integer, Kart Integer, DateTime Text, UNIQUE(HeatNo, CustId) ON CONFLICT REPLACE);
            //CREATE TABLE 'Laps'(HeatNo Integer, CustId Integer, Lap Integer, Time Real, UNIQUE(HeatNo, CustId, Lap) ON CONFLICT REPLACE);
            _database = new SQLiteDatabase(@"Data Source = C:\Users\Delmus\Documents\Visual Studio 2017\WebSites\ATXKarts\Bin\RaceDatabase_New");

            ClubSpeedAustinLive clubSpeedMonitor = new ClubSpeedAustinLive();
            clubSpeedMonitor.OnUpdate += ClubSpeedMonitor_OnUpdate;
            clubSpeedMonitor.StartPolling();

            //117748 - Beginning of 2017 - 1/1/2017 10:05 AM
            //123910 - Marchish
            //ParseRaceHistory(123905, 125156);
            //KartInfoTest();

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static void ClubSpeedMonitor_OnUpdate(LiveRaceInfo raceInfo)
        {
            if (raceInfo.ScoreboardData.Count == 0)
                return;

            if (raceInfo.RaceRunning)
                return;

            int heatNo = int.Parse(raceInfo.ScoreboardData[0].HeatNo);

            if (_observedHeatNos.Contains(heatNo))
                return;

            OnRaceFinished(heatNo);

            _observedHeatNos.Add(heatNo);
        }

        private static void KartInfoTest()
        {
            DataTable data = _database.GetDataTable(string.Format("SELECT Kart, RacerName, MIN(BestLap) AS BestLap, AVG(BestLap) AS AvgLap, DateTime FROM Races INNER JOIN Racers ON Races.CustId = Racers.Id GROUP BY Kart WHERE;", DateTime.Now.Subtract(TimeSpan.FromDays(7)).ToString("G")));

            foreach (DataRow row in data.Rows)
            {
                Console.WriteLine("Kart - {0} || {1} || {2} || {3} || {4}", row["Kart"], row["RacerName"], row["BestLap"], row["AvgLap"], row["DateTime"]);
            }
        }

        private static void ParseRaceHistory(int startHeatNo, int endHeatNo)
        {
            for (int i = startHeatNo; i < endHeatNo; i++)
            {
                Console.WriteLine("Processing HeatNo: {0}", i);
                ParseRaceHistory(i);
            }
        }

        private static void ParseRaceHistory(int heatNo)
        {
            HeatResult heat = ClubSpeedAustin.GetHeatResults(heatNo);

            if (heat == null)
                return;

            string racersQuery = "INSERT INTO Racers VALUES ";
            string racesQuery = "INSERT INTO Races VALUES";
            string lapsQuery = "INSERT INTO Laps VALUES";

            for (int i = 0; i < heat.Racers.Count; i++)
            {
                string racerName = heat.Racers[i].RacerName.Replace("'", "''");
                racersQuery += string.Format("('{0}', '{1}'){2}", heat.Racers[i].CustId, racerName, (i < heat.Racers.Count - 1) ? "," : ";");
                racesQuery += string.Format("('{0}', '{1}', '{2}', '{3}'){4}", heat.HeatNo, heat.Racers[i].CustId, heat.Racers[i].Kart, heat.DateTime.ToString("yyyy-MM-dd HH:mm:ss"), (i < heat.Racers.Count - 1) ? "," : ";");
            }

            for (int i = 0; i < heat.Laps.Count; i++)
            {
                lapsQuery += string.Format("('{0}', '{1}', '{2}', '{3}'){4}", heat.HeatNo, heat.Laps[i].CustId, heat.Laps[i].LapNum, heat.Laps[i].LapTime, (i < heat.Laps.Count - 1) ? "," : ";");
            }

            _database.ExecuteNonQuery(racersQuery + racesQuery + lapsQuery);
        }

        private static void OnRaceFinished(int heatNo)
        {
            ParseRaceHistory(heatNo);
        }
    }
}
