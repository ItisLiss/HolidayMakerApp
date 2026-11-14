using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HolidayMaker.Models;

namespace HolidayMaker.Services
{
    public class BokningService
    {
        private readonly string _connectionString = "Server=localhost; Port=3306; Database=HolidayMakerDB; Uid=root; Pwd=;";

        public (int BokningsID, int AntalNatter, decimal TotalPris) SkapaBokning(int kundId, int rumId, DateTime incheck, DateTime utcheck, int antalPersoner)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new MySqlCommand("sp_SkapaBokning", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@p_KundID", kundId);
                    command.Parameters.AddWithValue("@p_RumID", rumId);
                    command.Parameters.AddWithValue("@p_Incheck", incheck.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@p_Utcheck", utcheck.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@p_AntalPersoner", antalPersoner);

                    command.ExecuteNonQuery();
                }

                long bokningsId = 0;
                using (var idCmd = new MySqlCommand("SELECT LAST_INSERT_ID();", connection))
                {
                    bokningsId = Convert.ToInt64(idCmd.ExecuteScalar());
                }

                decimal prisPerNatt = 0;
                using (var priceCmd = new MySqlCommand("SELECT PrisPerNatt FROM Rum WHERE RumID = @rumId", connection))
                {
                    priceCmd.Parameters.AddWithValue("@rumId", rumId);
                    prisPerNatt = Convert.ToDecimal(priceCmd.ExecuteScalar());
                }

                int antalNatter = (utcheck.Date - incheck.Date).Days;
                decimal totalPris = antalNatter * prisPerNatt;

                return ((int)bokningsId, antalNatter, totalPris);
            }
        }

        public bool KundenFinns(int kundId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM kunder WHERE KundID = @kundId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@kundId", kundId);
                    long count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }
        public void VisaBokningarForKund(int kundId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT b.BokningsID, bo.Namn AS HotellNamn, bo.Ort, r.Rumsnummer, b.IncheckningsDatum, b.UtcheckningsDatum, b.AntalPersoner,
                        DATEDIFF(b.UtcheckningsDatum, b.IncheckningsDatum) * r.PrisPerNatt AS TotalPris
                    FROM bokningar b
                    JOIN Rum r ON b.RumID = r.RumID
                    JOIN Boenden bo ON r.BoendeID = bo.BoendeID
                    WHERE b.KundID = @kundId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@kundId", kundId);

                    using (var reader = command.ExecuteReader())
                    {
                        bool hasRows = false;
                        Console.WriteLine($"\n=== BOKNINGAR FÖR KUND ID {kundId} ===");

                        // VIKTIGT: while (reader.Read()) måste köras INNAN vi läser data!
                        while (reader.Read())
                        {
                            hasRows = true;
                            int bokningsId = reader.GetInt32("BokningsID");
                            string hotellNamn = reader.GetString("HotellNamn");
                            string ort = reader.GetString("Ort");
                            string rumsnummer = reader.GetString("Rumsnummer");
                            DateTime incheck = reader.GetDateTime("IncheckningsDatum");
                            DateTime utcheck = reader.GetDateTime("UtcheckningsDatum");
                            int antalPers = reader.GetInt32("AntalPersoner");
                            decimal totalPris = reader.GetDecimal("TotalPris");

                            Console.WriteLine($"[BokningsID: {bokningsId}] {hotellNamn} ({ort}) - Rum {rumsnummer} | {incheck:yyyy-MM-dd} till {utcheck:yyyy-MM-dd} | Personer: {antalPers} | Totalpris: {totalPris} kr");
                        }

                        if (!hasRows)
                        {
                            Console.WriteLine("Inga bokningar hittades för denna kund.");
                        }
                    }
                }
            }
        }
        public void VisaAllaBokningar()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                SELECT b.BokningsID, k.Namn AS KundNamn, bo.Namn AS HotellNamn, bo.Ort, r.Rumsnummer, b.IncheckningsDatum, b.UtcheckningsDatum, b.AntalPersoner,
                    DATEDIFF(b.UtcheckningsDatum, b.IncheckningsDatum) * r.PrisPerNatt AS TotalPris
                FROM bokningar b
                JOIN kunder k ON b.KundID = k.KundID
                JOIN Rum r ON b.RumID = r.RumID
                JOIN Boenden bo ON r.BoendeID = bo.BoendeID
                ORDER BY b.IncheckningsDatum ASC";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        bool hasRows = false;
                        Console.WriteLine("\n=== ALLA BOKNINGAR I SYSTEMET (SORTERADE EFTER DATUM) ===");

                        while (reader.Read())
                        {
                            hasRows = true;
                            Console.WriteLine($"[BokningsID: {reader.GetInt32("BokningsID")}] Kund: {reader.GetString("KundNamn")} | {reader.GetString("HotellNamn")} ({reader.GetString("Ort")}) | Rum: {reader.GetString("Rumsnummer")} | {reader.GetDateTime("IncheckningsDatum"):yyyy-MM-dd} till {reader.GetDateTime("UtcheckningsDatum"):yyyy-MM-dd} | Personer: {reader.GetInt32("AntalPersoner")} | Totalpris: {reader.GetDecimal("TotalPris")} kr");
                        }

                        if (!hasRows)
                        {
                            Console.WriteLine("Inga bokningar hittades i systemet.");
                        }
                    }
                }
            }
        }
        public void TaBortBokning(int bokningsId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = "DELETE FROM bokningar WHERE BokningsID = @bokningsId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@bokningsId", bokningsId);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"\nBokning med ID {bokningsId} har avbokats och tagits bort.");
                    }
                    else
                    {
                        Console.WriteLine("\nIngen bokning med det ID:t hittades.");
                    }
                }
            }
        }

        public void UppdateraAntalPersoner(int bokningsId, int nyAntalPersoner)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                string checkQuery = @"
                    SELECT r.Kapacitet 
                    FROM bokningar b 
                    JOIN Rum r ON b.RumID = r.RumID 
                    WHERE b.BokningsID = @bokningsId";

                using (var checkCmd = new MySqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@bokningsId", bokningsId);
                    var result = checkCmd.ExecuteScalar();

                    if (result == null)
                    {
                        Console.WriteLine("\nBokningen hittades inte.");
                        return;
                    }

                    int kapacitet = Convert.ToInt32(result);
                    if (nyAntalPersoner > kapacitet)
                    {
                        Console.WriteLine($"\nFel: Det nya antalet personer ({nyAntalPersoner}) överstiger rummets maximala kapacitet ({kapacitet}).");
                        return;
                    }
                }

                string updateQuery = "UPDATE bokningar SET AntalPersoner = @antal WHERE BokningsID = @bokningsId";
                using (var updateCmd = new MySqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@antal", nyAntalPersoner);
                    updateCmd.Parameters.AddWithValue("@bokningsId", bokningsId);
                    updateCmd.ExecuteNonQuery();
                    Console.WriteLine("\nAntalet personer har uppdaterats för bokningen!");
                }
            }
        }
        public void SkapaKund(string namn, string epost, string telefon)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Kunder (Namn, Email, Telefon) VALUES (@namn, @epost, @telefon)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@namn", namn);
                    command.Parameters.AddWithValue("@epost", epost);
                    command.Parameters.AddWithValue("@telefon", telefon);
                    
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}