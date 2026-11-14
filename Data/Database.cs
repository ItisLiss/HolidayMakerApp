using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using HolidayMaker.Models;

namespace HolidayMaker.Data
{
    public class Database
    {
        private readonly string _connectionString = "Server=localhost; Port=3306; Database=HolidayMakerDB; Uid=root; Pwd=;";

        public void TestConnection()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                Console.WriteLine("Databasanslutningen via klassen fungerar!");
            }
        }
    }
}