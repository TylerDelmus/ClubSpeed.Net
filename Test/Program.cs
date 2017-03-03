using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Data.SQLite;
using ClubSpeed.Net;

namespace Test
{
    class Program
    {
        private static SQLiteDatabase _database;

        static void Main(string[] args)
        { 
            _database = new SQLiteDatabase(@"Data Source = C:\Users\Delmus\Documents\Visual Studio 2017\WebSites\ATXKarts\Bin\RaceDatabase");

            //RaceHistoryTest(107184, 121247);
            KartInfoTest();

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static void KartInfoTest()
        {
            DataTable test = _database.GetDataTable(string.Format("SELECT Kart, RacerName, MIN(BestLap) AS BestLap, AVG(BestLap) AS AvgLap, DateTime FROM Races INNER JOIN Racers ON Races.CustId = Racers.Id GROUP BY Kart WHERE;", DateTime.Now.Subtract(TimeSpan.FromDays(7)).ToString("G")));
            foreach (DataRow row in test.Rows)
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

            _database.ExecuteNonQuery(string.Format("REPLACE INTO Racers VALUES('{0}', '{1}');", racer.CustomerId, racer.RacerName.Replace("'", "''")));

            foreach(RaceResult race in racer.Races)
            {
               // if(race.Time.Year == DateTime.Now.Year)
                    _database.ExecuteNonQuery(string.Format("INSERT INTO Races SELECT '{0}', '{1}', '{2}', '{3}', '{4}' WHERE NOT EXISTS (SELECT 1 FROM Races WHERE HeatNo={0} AND CustId={1});", race.HeatNo, racer.CustomerId, race.Kart, race.BestLap, race.Time));

                Thread.Sleep(10);
            }
        }
    }
}
