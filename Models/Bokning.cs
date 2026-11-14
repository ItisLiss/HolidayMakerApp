using System;

namespace HolidayMaker.Models
{
    public class Bokning
    {
        public int BokningsID { get; set; }
        public int KundID { get; set; }
        public int RumID { get; set; }
        public DateTime IncheckningsDatum { get; set; }
        public DateTime UtcheckningsDatum { get; set; }
        public int AntalPersoner { get; set; }
        public decimal TotalPris { get; set; }
    }
}