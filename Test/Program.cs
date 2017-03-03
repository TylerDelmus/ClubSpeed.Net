using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using System.Data.SQLite;
using ClubSpeed.Net;

namespace Test
{
    class Program
    {
        private static SQLiteDatabase _database;
        private static List<int> _processedIds;

        static void Main(string[] args)
        { 
            _database = new SQLiteDatabase(@"Data Source = C:\Users\Delmus\Documents\Visual Studio 2017\WebSites\ATXKarts\Bin\RaceDatabase");
            _processedIds = new List<int>();

            /*
            for (int i = 107184; i < 121247; i++)
            {
                List<int> racers = null;

                Console.WriteLine("Processing HeatNo: {0}", i);

                try
                {
                    racers = ClubSpeedAustin.GetHeatRacerIds(i);
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} -- {1}", i, e.Message);
                }

                if (racers == null)
                    continue;

                foreach (int custId in racers)
                {
                    if (_processedIds.Contains(custId))
                        continue;

                    ProcessRacer(ClubSpeedAustin.GetRaceHistory(custId));
                    _processedIds.Add(custId);
                }

                Thread.Sleep(5);
            }
            */
            GetKartBasicInfo();

            Console.WriteLine("Done");
            Console.ReadLine();
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

        private static void GetKartBasicInfo()
        {
            DataTable test = _database.GetDataTable(string.Format("SELECT Kart, RacerName, MIN(BestLap) AS BestLap, AVG(BestLap) AS AvgLap, DateTime FROM Races INNER JOIN Racers ON Races.CustId = Racers.Id GROUP BY Kart WHERE;", DateTime.Now.Subtract(TimeSpan.FromDays(7)).ToString("G")));
            foreach (DataRow row in test.Rows)
            {
                Console.WriteLine("Kart - {0} || {1} || {2} || {3} || {4}", row["Kart"], row["RacerName"], row["BestLap"], row["AvgLap"], row["DateTime"]);
            }
        }
    }
}
