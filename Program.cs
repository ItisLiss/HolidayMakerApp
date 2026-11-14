using System;
using HolidayMaker.Services;
using HolidayMaker.Models;

class Program
{
    static void Main(string[] args)
    {
        var dbService = new DatabaseService();
        bool keepRunning = true;

        while (keepRunning)
        {
            Console.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("       VÄLKOMMEN TILL HOLIDAY MAKER     ");
            Console.WriteLine("========================================");
            Console.WriteLine("1. Visa alla boenden");
            Console.WriteLine("2. Sök lediga rum");
            Console.WriteLine("3. Boka ett rum");
            Console.WriteLine("4. Visa kunds bokningar");
            Console.WriteLine("5. Ändra antal personer i bokning");
            Console.WriteLine("6. Avboka");
            Console.WriteLine("7. Avsluta");
            Console.Write("\nVälj ett alternativ (1-7): ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    VisaAllaBoendenFlode(dbService);
                    break;
                case "2":
                    SokRumFlode(dbService);
                    break;
                case "3":
                    BokaRumFlode(dbService);
                    break;
                case "4":
                    VisaKundBokningarFlode(dbService);
                    break;
                case "5":
                    AndraBokningFlode(dbService);
                    break;
                case "6":
                    AvbokaFlode(dbService);
                    break;
                case "7":
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

    static void VisaAllaBoendenFlode(DatabaseService dbService)
    {
        Console.Clear();
        Console.WriteLine("=== ALLA BOENDEN ===");
        var boenden = dbService.GetAllBoenden();
        foreach (var b in boenden)
        {
            Console.WriteLine($"ID: {b.BoendeID} | {b.Namn} ({b.Ort}) | Pool: {(b.HarPool ? "Ja" : "Nej")} | Restaurang: {(b.HarRestaurang ? "Ja" : "Nej")} | Omdöme: {b.Omdome}/5 | Från {b.LagstaPrisPerNatt} kr/natt");
        }
        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }

    static void SokRumFlode(DatabaseService dbService)
    {
        Console.Clear();
        Console.WriteLine("=== SÖK LEDIGA RUM ===");
        
        Console.Write("Ange ort att söka i: ");
        string ort = Console.ReadLine();

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
        string sortering = Console.ReadLine()?.Trim().ToLower();

        dbService.SokLedigaRum(ort, incheck, utcheck, antalPers, pool, restaurang, avstandStrand, avstandCentrum, sortering);

        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }

    static void BokaRumFlode(DatabaseService dbService)
    {
        Console.Clear();
        Console.WriteLine("=== SKAPA BOKNING ===");
        
        Console.Write("Ange KundID: ");
        if (!int.TryParse(Console.ReadLine(), out int kundId)) return;

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
            dbService.SkapaBokning(kundId, rumId, incheck, utcheck, antalPers);
            Console.WriteLine("\nBokningen skapades och sparades i databasen!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nKunde inte genomföra bokningen: {ex.Message}");
        }

        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }

    static void VisaKundBokningarFlode(DatabaseService dbService)
    {
        Console.Clear();
        Console.WriteLine("=== VISA KUNDENS BOKNINGAR ===");
        Console.WriteLine("Tillgängliga kunder: 1: Anna, 2: Björn, 3: Cecilia, 4: David, 5: Erika");
        Console.Write("\nAnge KundID: ");
        
        if (!int.TryParse(Console.ReadLine(), out int kundId)) return;

        dbService.VisaBokningarForKund(kundId);

        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }

    static void AndraBokningFlode(DatabaseService dbService)
    {
        Console.Clear();
        Console.WriteLine("=== ÄNDRA ANTAL PERSONER ===");
        Console.Write("Ange BokningsID: ");
        if (!int.TryParse(Console.ReadLine(), out int bokningsId)) return;

        Console.Write("Ange nytt antal personer: ");
        if (!int.TryParse(Console.ReadLine(), out int nyAntal)) return;

        dbService.UppdateraAntalPersoner(bokningsId, nyAntal);

        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }

    static void AvbokaFlode(DatabaseService dbService)
    {
        Console.Clear();
        Console.WriteLine("=== AVBOKA / TA BORT BOKNING ===");
        Console.Write("Ange BokningsID som ska avbokas: ");
        if (!int.TryParse(Console.ReadLine(), out int bokningsId)) return;

        dbService.TaBortBokning(bokningsId);

        Console.WriteLine("\nTryck på valfri knapp för att återgå.");
        Console.ReadKey();
    }
}