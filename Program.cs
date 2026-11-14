using System;
using HolidayMaker.Services;
using HolidayMaker.Models;

class Program
{
    static void Main(string[] args)
    {
        var boendeService = new BoendeService();
        var bokningService = new BokningService();
        bool keepRunning = true;

        while (keepRunning)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("        VÄLKOMMEN TILL HOLIDAY MAKER    ");
            Console.WriteLine("========================================");
            Console.WriteLine("1. Visa alla boenden");
            Console.WriteLine("2. Sök lediga rum");
            Console.WriteLine("3. Boka ett rum");
            Console.WriteLine("4. Visa kunds bokningar");
            Console.WriteLine("5. Ändra antal personer i bokning");
            Console.WriteLine("6. Avboka");
            Console.WriteLine("7. Registrera ny kund");
            Console.WriteLine("8. Avsluta");
            Console.Write("\nVälj ett alternativ (1-8): ");

            string choice = Console.ReadLine() ?? string.Empty;

            switch (choice)
            {
                case "1":
                    VisaAllaBoendenFlode(boendeService);
                    break;
                case "2":
                    SokRumFlode(boendeService);
                    break;
                case "3":
                    BokaRumFlode(boendeService, bokningService);
                    break;
                case "4":
                VisaBokningarMenyFlode(bokningService);
                break;
                case "5":
                    AndraBokningFlode(bokningService);
                    break;
                case "6":
                    AvbokaFlode(bokningService);
                    break;
                case "7":
                    RegistreraKundFlode(bokningService);
                    break;
                case "8":
                    keepRunning = false;
                    Console.WriteLine("\nTack för att du använde Holiday Maker. Välkommen åter!");
                    break;
                default:
                    Console.WriteLine("\nOgiltigt val. Tryck på valfri knapp för att försöka igen.");
                    Console.ReadKey();
                    break;
            }
        }
    }

    static void VisaAllaBoendenFlode(BoendeService boendeService)
    {
        Console.Clear();
        Console.WriteLine("=== ALLA BOENDEN ===");
        var boenden = boendeService.GetAllBoenden();
        foreach (var b in boenden)
        {
            Console.WriteLine($"ID: {b.BoendeID} | {b.Namn} ({b.Ort}) | Pool: {(b.HarPool ? "Ja" : "Nej")} | Restaurang: {(b.HarRestaurang ? "Ja" : "Nej")} | Omdöme: {b.Omdome}/5 | Från {b.LagstaPrisPerNatt} kr/natt");
        }
        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }

    static void SokRumFlode(BoendeService boendeService)
    {
        Console.Clear();
        Console.WriteLine("=== SÖK LEDIGA RUM ===");

        var städer = boendeService.GetUnikaStäder();
        Console.WriteLine("Tillgängliga orter: " + string.Join(", ", städer));
        
        Console.Write("Ange ort att söka i: ");
        string ort = Console.ReadLine() ?? string.Empty;

        Console.Write("Incheckningsdatum (YYYY-MM-DD): ");
        if (!DateTime.TryParse(Console.ReadLine(), out DateTime incheck))
        {
            Console.WriteLine("Ogiltigt datum.");
            Console.ReadKey();
            return;
        }

        Console.Write("Utcheckningsdatum (YYYY-MM-DD): ");
        if (!DateTime.TryParse(Console.ReadLine(), out DateTime utcheck))
        {
            Console.WriteLine("Ogiltigt datum.");
            Console.ReadKey();
            return;
        }

        Console.Write("Antal personer: ");
        if (!int.TryParse(Console.ReadLine(), out int antalPers))
        {
            Console.WriteLine("Ogiltigt antal.");
            Console.ReadKey();
            return;
        }

        Console.Write("Krävs pool? (j/n): ");
        bool pool = Console.ReadLine()?.Trim().ToLower() == "j";

        Console.Write("Krävs restaurang? (j/n): ");
        bool restaurang = Console.ReadLine()?.Trim().ToLower() == "j";

        Console.Write("Max avstånd till strand (meter, 0 om ej viktigt): ");
        int.TryParse(Console.ReadLine(), out int avstandStrand);

        Console.Write("Max avstånd till centrum (meter, 0 om ej viktigt): ");
        int.TryParse(Console.ReadLine(), out int avstandCentrum);

        Console.Write("Sortera efter ('pris' för lägst pris, 'omdome' för högst omdöme, eller enter för ingen): ");
        string sortering = Console.ReadLine()?.Trim().ToLower() ?? string.Empty;

        boendeService.SokLedigaRum(ort, incheck, utcheck, antalPers, pool, restaurang, avstandStrand, avstandCentrum, sortering);

        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }

    static void BokaRumFlode(BoendeService boendeService, BokningService bokningService)
    {
        Console.Clear();
        Console.WriteLine("=== SKAPA BOKNING ===");
        
        Console.Write("Ange KundID: ");
        if (!int.TryParse(Console.ReadLine(), out int kundId)) return;

        if (!bokningService.KundenFinns(kundId))
        {
            Console.WriteLine($"\nFel: Kunde med ID {kundId} existerar inte i databasen.");
            Console.WriteLine("\nTryck på valfri knapp för att återgå.");
            Console.ReadKey();
            return;
        }

        Console.Write("Ange RumID som ska bokas: ");
        if (!int.TryParse(Console.ReadLine(), out int rumId)) return;

        Console.Write("Incheckningsdatum (YYYY-MM-DD): ");
        if (!DateTime.TryParse(Console.ReadLine(), out DateTime incheck)) return;

        Console.Write("Utcheckningsdatum (YYYY-MM-DD): ");
        if (!DateTime.TryParse(Console.ReadLine(), out DateTime utcheck)) return;

        Console.Write("Antal personer: ");
        if (!int.TryParse(Console.ReadLine(), out int antalPers)) return;

        try
        {
            var bekraftelse = bokningService.SkapaBokning(kundId, rumId, incheck, utcheck, antalPers);
            
            Console.WriteLine("\n========================================");
            Console.WriteLine("          BOKNINGSBEKRÄFTELSE           ");
            Console.WriteLine("========================================");
            Console.WriteLine($"Bokningsnummer: {bekraftelse.BokningsID}");
            Console.WriteLine($"Antal nätter:   {bekraftelse.AntalNatter}");
            Console.WriteLine($"Totalpris:      {bekraftelse.TotalPris} kr");
            Console.WriteLine("========================================");
            Console.WriteLine("\nBokningen skapades och sparades i databasen!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nKunde inte genomföra bokningen: {ex.Message}");
        }

        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }

    static void VisaBokningarMenyFlode(BokningService bokningService)
    {
        Console.Clear();
        Console.WriteLine("=== VISA BOKNINGAR ===");
        Console.WriteLine("1. Visa alla bokningar i systemet (sorterade efter datum)");
        Console.WriteLine("2. Visa bokningar för en specifik kund");
        Console.Write("\nVälj ett alternativ (1-2): ");

        string val = Console.ReadLine() ?? string.Empty;

        if (val == "1")
        {
            Console.Clear();
            bokningService.VisaAllaBokningar();
        }
        else if (val == "2")
        {
            Console.Clear();
            Console.WriteLine("=== VISA KUNDENS BOKNINGAR ===");
            Console.WriteLine("Tillgängliga kunder: 1: Anna, 2: Björn, 3: Cecilia, 4: David, 5: Erika");
            Console.Write("\nAnge KundID: ");
            
            if (!int.TryParse(Console.ReadLine(), out int kundId)) return;

            bokningService.VisaBokningarForKund(kundId);
        }
        else
        {
            Console.WriteLine("\nOgiltigt val.");
        }

        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }

    static void AndraBokningFlode(BokningService bokningService)
    {
        Console.Clear();
        Console.WriteLine("=== ÄNDRA ANTAL PERSONER ===");
        Console.Write("Ange BokningsID: ");
        if (!int.TryParse(Console.ReadLine(), out int bokningsId)) return;

        Console.Write("Ange nytt antal personer: ");
        if (!int.TryParse(Console.ReadLine(), out int nyAntal)) return;

        bokningService.UppdateraAntalPersoner(bokningsId, nyAntal);

        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }

    static void AvbokaFlode(BokningService bokningService)
    {
        Console.Clear();
        Console.WriteLine("=== AVBOKA / TA BORT BOKNING ===");
        Console.Write("Ange BokningsID som ska avbokas: ");
        if (!int.TryParse(Console.ReadLine(), out int bokningsId)) return;

        bokningService.TaBortBokning(bokningsId);

        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }
   static void RegistreraKundFlode(BokningService bokningService)
    {
        Console.Clear();
        Console.WriteLine("=== REGISTRERA NY KUND ===");
        
        Console.Write("Ange kundens namn: ");
        string namn = Console.ReadLine() ?? string.Empty;

        Console.Write("Ange e-post: ");
        string epost = Console.ReadLine() ?? string.Empty;

        Console.Write("Ange telefonnummer: ");
        string telefon = Console.ReadLine() ?? string.Empty;

        try
        {
            bokningService.SkapaKund(namn, epost, telefon);
            Console.WriteLine("\nKunden har sparats i databasen!");
            Console.WriteLine("\nGlöm inte att uppdatera databasen för att få tillgång till nya kunder!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nKunde inte registrera kund: {ex.Message}");
        }

        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }
}