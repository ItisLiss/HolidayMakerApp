namespace HolidayMaker.Models
{
    public class Boende
    {
        public int BoendeID { get; set; }
        public string Namn { get; set; } = string.Empty;
        public string Ort { get; set; } = string.Empty;
        public bool HarPool { get; set; }
        public bool HarRestaurang { get; set; }
        public int AvstandStrand { get; set; }
        public int AvstandCentrum { get; set; }
        public int Omdome { get; set; }
        
        public decimal LagstaPrisPerNatt { get; set; }
        public int AntalRum { get; set; }
    }
}