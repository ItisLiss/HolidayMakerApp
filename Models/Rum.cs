namespace HolidayMaker.Models
{
    public class Rum
    {
        public int RumID { get; set; }
        public int BoendeID { get; set; }
        public string Rumsnummer { get; set; } = string.Empty;
        public int Kapacitet { get; set; }
        public decimal PrisPerNatt { get; set; }
    }
}