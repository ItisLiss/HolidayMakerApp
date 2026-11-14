using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HolidayMaker.Models;

namespace HolidayMaker.Services
{
    public class BoendeService
    {
        private readonly string _connectionString = "Server=localhost; Port=3306; Database=HolidayMakerDB; Uid=root; Pwd=;";

        public List<Boende> GetAllBoenden()
        {
            var boenden = new List<Boende>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM vw_Boendedetaljer";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            boenden.Add(new Boende
                            {
                                BoendeID = reader.GetInt32("BoendeID"),
                                Namn = reader.GetString("BoendenNamn"),
                                Ort = reader.GetString("Ort"),
                                HarPool = reader.GetBoolean("HarPool"),
                                HarRestaurang = reader.GetBoolean("HarRestaurang"),
                                AvstandStrand = reader.GetInt32("AvstandStrand"),
                                AvstandCentrum = reader.GetInt32("AvstandCentrum"),
                                Omdome = reader.GetInt32("Omdome"),
                                LagstaPrisPerNatt = reader.IsDBNull(reader.GetOrdinal("LagstaPrisPerNatt")) ? 0 : reader.GetDecimal("LagstaPrisPerNatt"),
                                AntalRum = reader.IsDBNull(reader.GetOrdinal("AntalRum")) ? 0 : reader.GetInt32("AntalRum")
                            });
                        }
                    }
                }
            }

            return boenden;
        }

        public List<Boende> GetBoendenByOrt(string ort)
        {
            var boenden = new List<Boende>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM vw_Boendedetaljer WHERE Ort LIKE @ort";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ort", $"%{ort}%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            boenden.Add(new Boende
                            {
                                BoendeID = reader.GetInt32("BoendeID"),
                                Namn = reader.GetString("BoendenNamn"),
                                Ort = reader.GetString("Ort"),
                                HarPool = reader.GetBoolean("HarPool"),
                                HarRestaurang = reader.GetBoolean("HarRestaurang"),
                                AvstandStrand = reader.GetInt32("AvstandStrand"),
                                AvstandCentrum = reader.GetInt32("AvstandCentrum"),
                                Omdome = reader.GetInt32("Omdome"),
                                LagstaPrisPerNatt = reader.IsDBNull(reader.GetOrdinal("LagstaPrisPerNatt")) ? 0 : reader.GetDecimal("LagstaPrisPerNatt"),
                                AntalRum = reader.IsDBNull(reader.GetOrdinal("AntalRum")) ? 0 : reader.GetInt32("AntalRum")
                            });
                        }
                    }
                }
            }

            return boenden;
        }

        public List<Boende> GetBoendenWithFilters(string ort, bool endastPool, int maxAvstandStrand)
        {
            var boenden = new List<Boende>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                string query = @"SELECT * 
                               FROM vw_BoendeDetaljer 
                               WHERE Ort LIKE @ort 
                               AND (@endastPool = 0 OR HarPool = 1) 
                               AND AvstandStrand <= @maxAvstand
                               AND LagstaPrisPerNatt > 0";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ort", $"%{ort}%");
                    command.Parameters.AddWithValue("@endastPool", endastPool ? 1 : 0);
                    command.Parameters.AddWithValue("@maxAvstand", maxAvstandStrand);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            boenden.Add(new Boende
                            {
                                BoendeID = reader.GetInt32("BoendeID"),
                                Namn = reader.GetString("BoendenNamn"),
                                Ort = reader.GetString("Ort"),
                                HarPool = reader.GetBoolean("HarPool"),
                                HarRestaurang = reader.GetBoolean("HarRestaurang"),
                                AvstandStrand = reader.GetInt32("AvstandStrand"),
                                AvstandCentrum = reader.GetInt32("AvstandCentrum"),
                                Omdome = reader.GetInt32("Omdome"),
                                LagstaPrisPerNatt = reader.IsDBNull(reader.GetOrdinal("LagstaPrisPerNatt")) ? 0 : reader.GetDecimal("LagstaPrisPerNatt"),
                                AntalRum = reader.IsDBNull(reader.GetOrdinal("AntalRum")) ? 0 : reader.GetInt32("AntalRum")
                            });
                        }
                    }
                }
            }

            return boenden;
        }

        public void SokLedigaRum(string ort, DateTime incheck, DateTime utcheck, int antalPersoner, bool endastPool, bool endastRestaurang, int maxAvstandStrand, int maxAvstandCentrum, string sortering)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT r.RumID, bo.Namn AS HotellNamn, bo.Ort, r.Rumsnummer, r.Kapacitet, r.PrisPerNatt, bo.Omdome, bo.HarPool, bo.HarRestaurang, bo.AvstandStrand, bo.AvstandCentrum
                    FROM Rum r
                    JOIN Boenden bo ON r.BoendeID = bo.BoendeID
                    WHERE bo.Ort LIKE @ort
                    AND r.Kapacitet >= @antalPersoner
                    AND NOT EXISTS (
                        SELECT 1 FROM bokningar b 
                        WHERE b.RumID = r.RumID 
                            AND b.IncheckningsDatum < @utcheck 
                            AND b.UtcheckningsDatum > @incheck
                    )";

                if (endastPool)
                {
                    query += " AND bo.HarPool = 1";
                }
                if (endastRestaurang)
                {
                    query += " AND bo.HarRestaurang = 1";
                }
                if (maxAvstandStrand > 0)
                {
                    query += " AND bo.AvstandStrand <= @maxAvstandStrand";
                }
                if (maxAvstandCentrum > 0)
                {
                    query += " AND bo.AvstandCentrum <= @maxAvstandCentrum";
                }

                if (sortering == "pris")
                {
                    query += " ORDER BY r.PrisPerNatt ASC";
                }
                else if (sortering == "omdome")
                {
                    query += " ORDER BY bo.Omdome DESC";
                }

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ort", $"%{ort}%");
                    command.Parameters.AddWithValue("@incheck", incheck.Date);
                    command.Parameters.AddWithValue("@utcheck", utcheck.Date);
                    command.Parameters.AddWithValue("@antalPersoner", antalPersoner);
                    
                    if (maxAvstandStrand > 0) command.Parameters.AddWithValue("@maxAvstandStrand", maxAvstandStrand);
                    if (maxAvstandCentrum > 0) command.Parameters.AddWithValue("@maxAvstandCentrum", maxAvstandCentrum);

                    using (var reader = command.ExecuteReader())
                    {
                        bool hasRows = false;
                        Console.WriteLine("\n--- SÖKRESULTAT: LEDIGA RUM ---");

                        while (reader.Read())
                        {
                            hasRows = true;
                            Console.WriteLine($"[RumID: {reader.GetInt32("RumID")}] {reader.GetString("HotellNamn")} ({reader.GetString("Ort")}) | Rum: {reader.GetString("Rumsnummer")} | Kapacitet: {reader.GetInt32("Kapacitet")} pers | Pris: {reader.GetDecimal("PrisPerNatt")} kr/natt | Omdöme: {reader.GetInt32("Omdome")}/5");
                        }

                        if (!hasRows)
                        {
                            Console.WriteLine("Inga lediga rum matchade dina sökkriterier.");
                        }
                    }
                }
            }
        }

        public List<Rum> GetRumForBoende(int boendeId)
        {
            var rumList = new List<Rum>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT RumID, BoendeID, Rumsnummer, Kapacitet, PrisPerNatt FROM Rum WHERE BoendeID = @boendeId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@boendeId", boendeId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rumList.Add(new Rum
                            {
                                RumID = reader.GetInt32("RumID"),
                                BoendeID = reader.GetInt32("BoendeID"),
                                Rumsnummer = reader.GetString("Rumsnummer"),
                                Kapacitet = reader.GetInt32("Kapacitet"),
                                PrisPerNatt = reader.GetDecimal("PrisPerNatt")
                            });
                        }
                    }
                }
            }

            return rumList;
        }

        public List<string> GetUnikaStäder()
        {
            var städer = new List<string>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT DISTINCT Ort FROM Boenden";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            städer.Add(reader.GetString("Ort"));
                        }
                    }
                }
            }

            return städer;
        }
    }
}