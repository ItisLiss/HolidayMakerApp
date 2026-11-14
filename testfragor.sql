-- ==========================================
-- HOLIDAY MAKER - TESTFRÅGOR FÖR SQL
-- ==========================================

USE HolidayMakerDB; 

-- 1. Hitta alla boenden som har pool och är nära stranden (< 300m)
SELECT Namn, Ort, AvstandStrand 
FROM Boenden 
WHERE HarPool = 1 AND AvstandStrand < 300;

-- 2. Hitta alla boenden i en specifik stad (som Göteborg)
SELECT * FROM Boenden 
WHERE Ort = 'Göteborg';

-- 3. Hämta alla rum som kostar mindre än 1000 kr per natt
SELECT r.Rumsnummer, b.Namn AS Boende, r.PrisPerNatt 
FROM Rum r
JOIN Boenden b ON r.BoendeID = b.BoendeID
WHERE r.PrisPerNatt < 1000;

-- 4. Se alla bokningar med kundnamn, boende, rumsnummer och totalpris (JOIN av 4 tabeller)
SELECT 
    b.BokningsID, 
    k.Namn AS Kund, 
    bo.Namn AS Boende, 
    r.Rumsnummer, 
    b.IncheckningsDatum, 
    b.UtcheckningsDatum, 
    b.TotalPris
FROM Bokningar b
JOIN Kunder k ON b.KundID = k.KundID
JOIN Rum r ON b.RumID = r.RumID
JOIN Boenden bo ON r.BoendeID = bo.BoendeID;

-- 5. Se alla bokningar för en specifik kund (t.ex. Anna Andersson)
SELECT 
    b.BokningsID, 
    bo.Namn AS Boende, 
    b.IncheckningsDatum, 
    b.UtcheckningsDatum, 
    b.TotalPris
FROM Bokningar b
JOIN Kunder k ON b.KundID = k.KundID
JOIN Rum r ON b.RumID = r.RumID
JOIN Boenden bo ON r.BoendeID = bo.BoendeID
WHERE k.Namn = 'Anna Andersson';

-- 6. Räkna hur många boenden som finns i varje stad
SELECT Ort, COUNT(*) AS AntalBoenden 
FROM Boenden 
GROUP BY Ort;

-- 7. Visa genomsnittspriset per natt för rummen per boende
SELECT 
    b.Namn AS Boende, 
    ROUND(AVG(r.PrisPerNatt), 2) AS SnittPris 
FROM Rum r
JOIN Boenden b ON r.BoendeID = b.BoendeID
GROUP BY b.Namn;

-- 8. Testa tillgänglighetskoll (Hitta rum som INTE är upptagna under vald period)
SELECT r.RumID, r.Rumsnummer, b.Namn AS Boende
FROM Rum r
JOIN Boenden b ON r.BoendeID = b.BoendeID
WHERE b.Ort = 'Stockholm'
  AND NOT EXISTS (
      SELECT 1 FROM Bokningar bk 
      WHERE bk.RumID = r.RumID 
        AND bk.IncheckningsDatum < '2026-12-20' 
        AND bk.UtcheckningsDatum > '2026-12-15'
  );


-- ========================================================
-- HOLIDAY MAKER - VYER, PROCEDURER OCH ANVÄNDAR-RÄTTIGHETER
-- ========================================================

USE HolidayMakerDB;
-- Började använda för att inte skapa dubletter från tidigare övningar
DROP VIEW IF EXISTS vw_BoendeDetaljer;
-- Jag vill se alla Boende detaljer, även omdömme (dock inte fått med under filtreringen i  C#)
CREATE VIEW vw_BoendeDetaljer AS
SELECT 
    b.BoendeID,
    b.Namn AS BoendeNamn,
    b.Ort,
    b.HarPool,
    b.HarRestaurang,
    b.AvstandStrand,
    b.AvstandCentrum,
    b.Omdome,
    MIN(r.PrisPerNatt) AS LagstaPris,
    COUNT(r.RumID) AS AntalRum
FROM Boenden b
LEFT JOIN Rum r ON b.BoendeID = r.BoendeID
GROUP BY b.BoendeID, b.Namn, b.Ort, b.HarPool, b.HarRestaurang, b.AvstandStrand, b.AvstandCentrum, b.Omdome;

-- Vy för rumsdetaljer per boende (Används när användaren valt ett hotell och ska se lediga/tillgängliga rum)
CREATE OR REPLACE VIEW vw_RumMedBoende AS
SELECT 
    r.RumID,
    r.BoendeID,
    b.Namn AS BoendeNamn,
    b.Ort,
    r.Rumsnummer,
    r.Kapacitet,
    r.PrisPerNatt
FROM Rum r
JOIN Boenden b ON r.BoendeID = b.BoendeID;

-- Vy för kundens bokningar (Används i C# under "Mina bokningar" för att slippa 4-tabellers JOIN i koden)
DROP VIEW IF EXISTS vw_KundBokningar;
CREATE VIEW vw_KundBokningar AS
SELECT 
    b.BokningsID,
    k.Namn AS KundNamn,
    k.Email,
    bo.Namn AS BoendeNamn,
    bo.Ort,
    r.Rumsnummer,
    b.IncheckningsDatum,
    b.UtcheckningsDatum,
    b.AntalPersoner,
    b.TotalPris
FROM Bokningar b
JOIN Kunder k ON b.KundID = k.KundID
JOIN Rum r ON b.RumID = r.RumID
JOIN Boenden bo ON r.BoendeID = bo.BoendeID;

-- Jag vill se en sammanställning per stad och filtrera pris
CREATE VIEW vw_OrtsStatistik AS
SELECT 
    b.Ort,
    COUNT(DISTINCT b.BoendeID) AS AntalBoenden,
    COUNT(r.RumID) AS TotaltAntalRum,
    MIN(r.PrisPerNatt) AS LägstaPris
FROM Boenden b
LEFT JOIN Rum r ON b.BoendeID = r.BoendeID
GROUP BY b.Ort;

-- Finns fler vyer i Databasen
-- Skapa bokning och beräkna totalpris automatiskt
DROP PROCEDURE IF EXISTS sp_SkapaBokning;

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
    DECLARE v_AntalNatter INT;
    DECLARE v_TotalPris DECIMAL(10,2);

    -- Hämta rummets pris per natt
    SELECT PrisPerNatt INTO v_PrisPerNatt FROM Rum WHERE RumID = p_RumID;

    -- Beräkna antal nätter
    SET v_AntalNatter = DATEDIFF(p_Utcheck, p_Incheck);

    -- Beräkna totalpris
    SET v_TotalPris = v_PrisPerNatt * v_AntalNatter;

    -- Lägg till bokningen
    INSERT INTO Bokningar (KundID, RumID, IncheckningsDatum, UtcheckningsDatum, AntalPersoner, TotalPris)
    VALUES (p_KundID, p_RumID, p_Incheck, p_Utcheck, p_AntalPersoner, v_TotalPris);
END //

DELIMITER ;

-- Skapa en begränsad användare för applikationen
CREATE USER IF NOT EXISTS 'app_user'@'localhost' IDENTIFIED BY 'SakertLosenord123!';

-- Ge app_user enbart läsrättigheter samt skrivrättighet på bokningar/kunder
GRANT SELECT ON HolidayMakerDB.* TO 'app_user'@'localhost';
GRANT INSERT ON HolidayMakerDB.Bokningar TO 'app_user'@'localhost';
GRANT INSERT ON HolidayMakerDB.Kunder TO 'app_user'@'localhost';

FLUSH PRIVILEGES;

-- ========================================================
-- C#-APPILIKATIONENS(Dynamiska sökningar)
-- ========================================================

-- Hämta alla boenden grupperade/filtrerade på ort (används vid stadssök)
SELECT * FROM vw_BoendeDetaljer WHERE Ort LIKE '%Stockholm%';

-- Hämta unika städer för sökmenyn
SELECT DISTINCT Ort FROM Boenden;

-- Hämta alla rum för ett specifikt boende
SELECT RumID, BoendeID, Rumsnummer, Kapacitet, PrisPerNatt 
FROM Rum 
WHERE BoendeID = 1;

-- Sök boenden med flera kombinerade filter (Ort + Pool + Avstånd till strand)
-- (Exempel på hur C#-koden filtrerar direkt i databasen)
SELECT * 
FROM vw_BoendeDetaljer 
WHERE Ort LIKE '%Stockholm%' 
  AND (1 = 0 OR HarPool = 1) 
  AND AvstandStrand <= 500
  AND LagstaPrisPerNatt > 0;

-- Visa en specifik kunds bokningar (Används i 'Visa kunds bokningar' med JOIN)
SELECT 
    b.BokningsID, 
    bo.Namn AS HotellNamn, 
    bo.Ort, 
    r.Rumsnummer, 
    b.IncheckningsDatum, 
    b.UtcheckningsDatum, 
    b.AntalPersoner 
FROM bokningar b
JOIN Rum r ON b.RumID = r.RumID
JOIN Boenden bo ON r.BoendeID = bo.BoendeID
WHERE b.KundID = 1;

-- Ändra antal personer i en bokning (Körs i C# i två steg: kapacitetskoll + update)

-- Steg 1: Kontrollera att det nya antalet personer ryms inom rummets maxkapacitet
SELECT r.Kapacitet 
FROM bokningar b 
JOIN Rum r ON b.RumID = r.RumID 
WHERE b.BokningsID = 1;

-- Steg 2: Uppdatera antalet personer om kapaciteten räcker till
UPDATE bokningar 
SET AntalPersoner = 3 
WHERE BokningsID = 1;
-- --------------------------------------------------------
-- UNDERHÅLL, FELSÖKNING OCH STÄDNING (HISTORIK)
-- --------------------------------------------------------

  -- EXTRA: Upptäckte dubletter vid inmatning av Tabeller, vi börjar med beonden.
SELECT Namn, Ort, COUNT(*) AS AntalDubbletter
FROM Boenden
GROUP BY Namn, Ort
HAVING COUNT(*) > 1;

-- Kolla över alla boenden igen
SELECT BoendeID, Namn, Ort FROM Boenden ORDER BY Namn, BoendeID;

-- Radera dubletter
DELETE FROM Boenden 
WHERE BoendeID IN (
    10, 15, 20, 
    6, 11, 16, 
    9, 14, 19, 
    7, 12, 17, 
    8, 13, 18
);

-- Kollar rum 
SELECT RumID, BoendeID, Rumsnummer, PrisPerNatt FROM Rum ORDER BY BoendeID, Rumsnummer, RumID;
-- Raderar dubeltter
DELETE FROM Rum 
WHERE RumID BETWEEN 21 AND 80;

-- Kollar över kunder
SELECT Namn, Email, COUNT(*) AS AntalDubbletter
FROM Kunder
GROUP BY Namn, Email
HAVING COUNT(*) > 1;
-- Radera dubletter av kunder
DELETE FROM Kunder 
WHERE KundID BETWEEN 6 AND 20;

-- Bokningar
SELECT KundID, RumID, IncheckningsDatum, UtcheckningsDatum, COUNT(*) AS AntalDubbletter
FROM Bokningar
GROUP BY KundID, RumID, IncheckningsDatum, UtcheckningsDatum
HAVING COUNT(*) > 1;

-- Då AI fick hjälpa oss göra några snabba bokningar så valde jag att ta bort alla
TRUNCATE TABLE Bokningar;

-- --------------------------------------------------------
-- SÄKERHET OCH ANVÄNDARRÄTTIGHETER
-- --------------------------------------------------------

CREATE USER IF NOT EXISTS 'app_user'@'localhost' IDENTIFIED BY 'SakertLosenord123!';

GRANT SELECT ON HolidayMakerDB.* TO 'app_user'@'localhost';
GRANT INSERT ON HolidayMakerDB.Bokningar TO 'app_user'@'localhost';
GRANT INSERT ON HolidayMakerDB.Kunder TO 'app_user'@'localhost';

FLUSH PRIVILEGES;