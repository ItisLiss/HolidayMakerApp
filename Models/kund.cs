namespace HolidayMaker.Models
{
    public class Kund
    {
        public int KundID { get; set; }
        public string Namn { get; set; } = string.Empty;
        public string? Telefon { get; set; }
        public string? Email { get; set; }
    }
}