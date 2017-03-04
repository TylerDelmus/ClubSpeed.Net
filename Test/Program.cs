using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Data.SQLite;
using ClubSpeed.Net;

namespace Test
{
    public class Program
    {
        private static SQLiteDatabase _database;

        private static void Main(string[] args)
        {
            //This assumes a SQLite database with the following tables exists during these tests.
            //CREATE TABLE "Racers"("Id" Integer PRIMARY KEY NOT NULL, "RacerName" Text);
            //CREATE TABLE "Races"("HeatNo" Integer, "CustId" Integer, "Kart" Integer, "BestLap" Real, "DateTime" Text);
            //_database = new SQLiteDatabase(@"Data Source = C:\Users\Delmus\Documents\Visual Studio 2017\WebSites\ATXKarts\Bin\RaceDatabase");

            ClubSpeedAustinLive clubSpeedMonitor = new ClubSpeedAustinLive();
            clubSpeedMonitor.OnUpdate += ClubSpeedMonitor_OnUpdate;
            clubSpeedMonitor.StartPolling();

            //RaceHistoryTest(107184, 121247);
            //KartInfoTest();

            Console.ReadLine();
        }

        private static void ClubSpeedMonitor_OnUpdate(LiveRaceInfo raceInfo)
        {
            if (raceInfo.ScoreboardData.Count == 0)
            {
                Console.WriteLine("No players in current race.");
                return;
            }

            Console.WriteLine(string.Format("Race Type: {0}, Racers: {1}, Laps: {2}, HeatNo: {3}", raceInfo.HeatTypeName, raceInfo.ScoreboardData.Count, raceInfo.LapsLeft, raceInfo.ScoreboardData[0].HeatNo));
        }

        private static void KartInfoTest()
        {
            DataTable data = _database.GetDataTable(string.Format("SELECT Kart, RacerName, MIN(BestLap) AS BestLap, AVG(BestLap) AS AvgLap, DateTime FROM Races INNER JOIN Racers ON Races.CustId = Racers.Id GROUP BY Kart WHERE;", DateTime.Now.Subtract(TimeSpan.FromDays(7)).ToString("G")));

            foreach (DataRow row in data.Rows)
            {
                Console.WriteLine("Kart - {0} || {1} || {2} || {3} || {4}", row["Kart"], row["RacerName"], row["BestLap"], row["AvgLap"], row["DateTime"]);
            }
        }

        private static void RaceHistoryTest(int startHeatNo, int endHeatNo)
        {
            List<int> processedIds = new List<int>();

            for (int i = startHeatNo; i < endHeatNo; i++)
            {
                Console.WriteLine("Processing HeatNo: {0}", i);

                try
                {
                    List<int> racers = ClubSpeedAustin.GetRacerIdsForHeat(i);

                    foreach (int custId in racers)
                    {
                        if (processedIds.Contains(custId))
                            continue;

                        ProcessRacer(ClubSpeedAustin.GetRaceHistory(custId));
                        processedIds.Add(custId);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error on heat {0}: {1}", i, e.Message);
                }

                Thread.Sleep(5);
            }
        }

        private static void ProcessRacer(RaceHistory racer)
        {
            Console.WriteLine("Processing racer {0} races {1}", racer.CustomerId, racer.Races.Count);

            //Escape any single quotes that might appear in someone's name.
            string racerName = racer.RacerName.Replace("'", "''");

            //Add the racer info if they do not already exist.
            _database.ExecuteNonQuery(string.Format("REPLACE INTO Racers VALUES('{0}', '{1}');", racer.CustomerId, racerName));

            foreach(RaceResult race in racer.Races)
            {
                //TODO: Properly handle the DateTime field to only insert races within a certain time frame.
                //if(race.Time.Year == DateTime.Now.Year)
                    _database.ExecuteNonQuery(string.Format("INSERT INTO Races SELECT '{0}', '{1}', '{2}', '{3}', '{4}' WHERE NOT EXISTS (SELECT 1 FROM Races WHERE HeatNo={0} AND CustId={1});", race.HeatNo, racer.CustomerId, race.Kart, race.BestLap, race.Time));

                //Slow down writes to the database as sending them too quickly causes problems.
                //Possibly combine it into a single large query?
                Thread.Sleep(10);
            }
        }
    }
}
