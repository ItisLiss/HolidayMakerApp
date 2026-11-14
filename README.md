# Holiday Maker – Projektinstruktioner
Denna REDAME fil beskriver hur du sätter upp den underliggande SQL-databasen för Holiday Maker och hur  du ska komma igång och kunna använda den till C#-konsolapplikationen.

## Steg 1: Skapa databasen och strukturen i DataGrip / MySQL
Öppna din databasklient (t.ex. DataGrip) och kör följande SQL-skript för att skapa databas, tabeller, vy, stored procedure och lägga in testdata:

# SQL
## -- 1. Skapa databas
CREATE DATABASE IF NOT EXISTS HolidayMakerDB;
USE HolidayMakerDB;

## -- 2. Skapa tabeller
CREATE TABLE IF NOT EXISTS Boenden (
    BoendeID INT AUTO_INCREMENT PRIMARY KEY,
    Namn VARCHAR(100) NOT NULL,
    Ort VARCHAR(100) NOT NULL,
    HarPool BOOLEAN NOT NULL,
    HarRestaurang BOOLEAN NOT NULL,
    AvstandStrand INT NOT NULL,
    AvstandCentrum INT NOT NULL,
    Omdome INT CHECK (Omdome BETWEEN 1 AND 5)
);

CREATE TABLE IF NOT EXISTS Rum (
    RumID INT AUTO_INCREMENT PRIMARY KEY,
    BoendeID INT NOT NULL,
    Rumsnummer VARCHAR(20) NOT NULL,
    Kapacitet INT NOT NULL,
    PrisPerNatt DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (BoendeID) REFERENCES Boenden(BoendeID) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Kunder (
    KundID INT AUTO_INCREMENT PRIMARY KEY,
    Namn VARCHAR(100) NOT NULL,
    Epost VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS bokningar (
    BokningsID INT AUTO_INCREMENT PRIMARY KEY,
    KundID INT NOT NULL,
    RumID INT NOT NULL,
    IncheckningsDatum DATE NOT NULL,
    UtcheckningsDatum DATE NOT NULL,
    AntalPersoner INT NOT NULL,
    TotalPris DECIMAL(10,2),
    FOREIGN KEY (KundID) REFERENCES Kunder(KundID) ON DELETE CASCADE,
    FOREIGN KEY (RumID) REFERENCES Rum(RumID) ON DELETE CASCADE
);

## -- 3. Skapa vy för boendedetaljer (inkluderar lägsta pris och antal rum)
CREATE OR REPLACE VIEW vw_BoendeDetaljer AS
SELECT 
    b.BoendeID,
    b.Namn,
    b.Ort,
    b.HarPool,
    b.HarRestaurang,
    b.AvstandStrand,
    b.AvstandCentrum,
    b.Omdome,
    MIN(r.PrisPerNatt) AS LagstaPrisPerNatt,
    COUNT(r.RumID) AS AntalRum
FROM Boenden b
LEFT JOIN Rum r ON b.BoendeID = r.BoendeID
GROUP BY b.BoendeID, b.Namn, b.Ort, b.HarPool, b.HarRestaurang, b.AvstandStrand, b.AvstandCentrum, b.Omdome;

## -- 4. Skapa Stored Procedure för att skapa bokningar på ett säkert sätt
DELIMITER //
CREATE PROCEDURE sp_SkapaBokning(
    IN p_KundID INT,
    IN p_RumID INT,
    IN p_Incheck DATE,
    IN p_Utcheck DATE,
    IN p_AntalPersoner INT
)
BEGIN
    DECLARE v_PrisPerNatt DECIMAL(10,2);
    DECLARE v_AntalNätter INT;
    DECLARE v_TotalPris DECIMAL(10,2);
    DECLARE v_Kapacitet INT;

    -- Kontrollera att rummets kapacitet räcker
    SELECT Kapacitet, PrisPerNatt INTO v_Kapacitet, v_PrisPerNatt
    FROM Rum WHERE RumID = p_RumID;

    IF p_AntalPersoner > v_Kapacitet THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Antalet personer överstiger rummets kapacitet.';
    END IF;

    -- Beräkna antal nätter och pris
    SET v_AntalNätter = DATEDIFF(p_Utcheck, p_Incheck);
    
    IF v_AntalNätter <= 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Utcheckningsdatum måste ligga efter incheckningsdatum.';
    END IF;

    SET v_TotalPris = v_AntalNätter * v_PrisPerNatt;

    -- Spara bokningen
    INSERT INTO bokningar (KundID, RumID, IncheckningsDatum, UtcheckningsDatum, AntalPersoner, TotalPris)
    VALUES (p_KundID, p_RumID, p_Incheck, p_Utcheck, p_AntalPersoner, v_TotalPris);
END //
DELIMITER ;

## -- 5. Lägg in testdata (Boenden, Rum, Kunder)
INSERT INTO Boenden (Namn, Ort, HarPool, HarRestaurang, AvstandStrand, AvstandCentrum, Omdome) VALUES
('Hotel Solglimt', 'Stockholm', TRUE, TRUE, 200, 500, 4),
('Fjälltopp Pensionat', 'Åre', FALSE, TRUE, 5000, 100, 5),
('Västkust Perlan', 'Göteborg', TRUE, FALSE, 50, 1200, 3),
('City Stay', 'Stockholm', FALSE, FALSE, 3000, 50, 4),
('Sjöutsikt Resort', 'Malmö', TRUE, TRUE, 100, 800, 5);

## -- Lägg in rum (fördelat på boendena med olika kapaciteter)
INSERT INTO Rum (BoendeID, Rumsnummer, Kapacitet, PrisPerNatt) VALUES
(1, '101', 2, 1200.00), (1, '102', 4, 1800.00), (1, '103', 1, 800.00),
(2, '201', 2, 1500.00), (2, '202', 4, 2500.00), (2, '203', 3, 2000.00),
(3, '301', 1, 950.00),  (3, '302', 2, 1350.00), (3, '303', 4, 2200.00),
(4, '401', 1, 700.00),  (4, '402', 2, 1100.00), (4, '403', 2, 1150.00),
(5, '501', 2, 1600.00), (5, '502', 4, 2800.00), (5, '503', 1, 1000.00),
(1, '104', 2, 1250.00), (2, '204', 4, 2600.00), (3, '304', 2, 1400.00), (5, '504', 2, 1700.00);

## -- Lägg in 5 testkunder
INSERT INTO Kunder (Namn, Epost) VALUES
('Anna Andersson', 'anna.andersson@example.com'),
('Björn Berg', 'bjorn.berg@example.com'),
('Cecilia Carlsson', 'cecilia.c@example.com'),
('David Dahlin', 'david.dahlin@example.com'),
('Erika Ek', 'erika.ek@example.com');
⚙️ Steg 2: Konfigurera anslutningen i C#
Öppna projektet i din IDE (t.ex. VS Code).

Gå till mappen Services och öppna filen DatabaseService.cs.

Kontrollera att anslutningssträngen längst upp i klassen matchar din lokala MySQL-miljö (användarnamn och lösenord):

## C#
private readonly string _connectionString = "Server=localhost; Port=3306; Database=HolidayMakerDB; Uid=root; Pwd=din_lösenord;";
▶️ Steg 3: Köra programmet
Öppna en terminal i projektets rotmapp (där .csproj-filen ligger).

Starta applikationen med kommandot:

Bash
dotnet run
Använd konsolmenyns alternativ (1–7) för att söka rum, boka, ändra antal personer, avboka eller granska kundernas bokningar.

## Extra notering: 
Detta är grunden till SQL databasen och innehåller inte sparade vyer.
För den fullständiga implementationen av C#‑logik, dynamiska sök- och filter-queries samt objektorienterade modeller, hänvisas till projektets källkodsfiler (såsom Program.cs och Services/DatabaseService.cs).