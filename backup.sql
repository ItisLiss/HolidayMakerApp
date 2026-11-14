create table if not exists boenden
(
    BoendeID       int auto_increment
        primary key,
    Namn           varchar(100) not null,
    Ort            varchar(50)  not null,
    HarPool        tinyint(1)   not null,
    HarRestaurang  tinyint(1)   not null,
    AvstandStrand  int          not null,
    AvstandCentrum int          not null,
    Omdome         int          null,
    check (`Omdome` between 1 and 5)
);

create table if not exists bokningar
(
    BokningsID        int auto_increment
        primary key,
    KundID            int            not null,
    RumID             int            not null,
    IncheckningsDatum date           not null,
    UtcheckningsDatum date           not null,
    AntalPersoner     int            not null,
    TotalPris         decimal(10, 2) not null,
    constraint bokningar_ibfk_1
        foreign key (KundID) references kunder (KundID),
    constraint bokningar_ibfk_2
        foreign key (RumID) references rum (RumID),
    constraint CHK_Datum
        check (`UtcheckningsDatum` > `IncheckningsDatum`)
);

create index KundID
    on bokningar (KundID);

create index RumID
    on bokningar (RumID);

grant insert on table bokningar to app_user@localhost;

create table if not exists kunder
(
    KundID  int auto_increment
        primary key,
    Namn    varchar(100) not null,
    Telefon varchar(30)  null,
    Email   varchar(100) null
);

grant insert on table kunder to app_user@localhost;

create table if not exists rum
(
    RumID       int auto_increment
        primary key,
    BoendeID    int            not null,
    Rumsnummer  varchar(20)    not null,
    Kapacitet   int            not null,
    PrisPerNatt decimal(10, 2) not null,
    constraint rum_ibfk_1
        foreign key (BoendeID) references boenden (BoendeID)
            on delete cascade
);

create index BoendeID
    on rum (BoendeID);

create
    definer = root@localhost procedure sp_SkapaBokning(IN p_KundID int, IN p_RumID int, IN p_Incheck date,
                                                       IN p_Utcheck date, IN p_AntalPersoner int)
BEGIN
    DECLARE v_PrisPerNatt DECIMAL(10,2);
    DECLARE v_AntalNatter INT;
    DECLARE v_TotalPris DECIMAL(10,2);
    DECLARE v_Kollisioner INT;

    -- 1. Kontrollera om rummet redan är bokat under den valda perioden
    SELECT COUNT(*) INTO v_Kollisioner
    FROM Bokningar
    WHERE RumID = p_RumID
      AND NOT (p_Utcheck <= IncheckningsDatum OR p_Incheck >= UtcheckningsDatum);

    IF v_Kollisioner > 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Fel: Rummet är redan bokat under valda datum.';
    END IF;

    -- 2. Hämta pris per natt och beräkna totalpris
    SELECT PrisPerNatt INTO v_PrisPerNatt FROM Rum WHERE RumID = p_RumID;
    SET v_AntalNatter = DATEDIFF(p_Utcheck, p_Incheck);
    SET v_TotalPris = v_PrisPerNatt * v_AntalNatter;

    -- 3. Spara bokningen
    INSERT INTO Bokningar (KundID, RumID, IncheckningsDatum, UtcheckningsDatum, AntalPersoner, TotalPris)
    VALUES (p_KundID, p_RumID, p_Incheck, p_Utcheck, p_AntalPersoner, v_TotalPris);
END;

create or replace definer = root@localhost view vw_boendedeatljer as
select `b`.`BoendeID`         AS `BoendeID`,
       `b`.`Namn`             AS `BoendenNamn`,
       `b`.`Ort`              AS `Ort`,
       `b`.`HarPool`          AS `HarPool`,
       `b`.`HarRestaurang`    AS `HarRestaurang`,
       `b`.`AvstandStrand`    AS `AvstandStrand`,
       `b`.`AvstandCentrum`   AS `AvstandCentrum`,
       `b`.`Omdome`           AS `Omdome`,
       min(`r`.`PrisPerNatt`) AS `LagstaPrisPerNatt`,
       count(`r`.`RumID`)     AS `AntalRum`
from (`holidaymakerdb`.`boenden` `b` left join `holidaymakerdb`.`rum` `r` on ((`b`.`BoendeID` = `r`.`BoendeID`)))
group by `b`.`BoendeID`, `b`.`Namn`, `b`.`Ort`, `b`.`HarPool`, `b`.`HarRestaurang`, `b`.`AvstandStrand`,
         `b`.`AvstandCentrum`, `b`.`Omdome`;

create or replace definer = root@localhost view vw_boendedetaljer as
select `b`.`BoendeID`         AS `BoendeID`,
       `b`.`Namn`             AS `BoendenNamn`,
       `b`.`Ort`              AS `Ort`,
       `b`.`HarPool`          AS `HarPool`,
       `b`.`HarRestaurang`    AS `HarRestaurang`,
       `b`.`AvstandStrand`    AS `AvstandStrand`,
       `b`.`AvstandCentrum`   AS `AvstandCentrum`,
       `b`.`Omdome`           AS `Omdome`,
       min(`r`.`PrisPerNatt`) AS `LagstaPrisPerNatt`,
       count(`r`.`RumID`)     AS `AntalRum`
from (`holidaymakerdb`.`boenden` `b` left join `holidaymakerdb`.`rum` `r` on ((`b`.`BoendeID` = `r`.`BoendeID`)))
group by `b`.`BoendeID`, `b`.`Namn`, `b`.`Ort`, `b`.`HarPool`, `b`.`HarRestaurang`, `b`.`AvstandStrand`,
         `b`.`AvstandCentrum`, `b`.`Omdome`;

create or replace definer = root@localhost view vw_boendefaciliteter as
select `holidaymakerdb`.`boenden`.`BoendeID`       AS `BoendeID`,
       `holidaymakerdb`.`boenden`.`Namn`           AS `BoendeNamn`,
       `holidaymakerdb`.`boenden`.`Ort`            AS `Ort`,
       `holidaymakerdb`.`boenden`.`HarPool`        AS `HarPool`,
       `holidaymakerdb`.`boenden`.`HarRestaurang`  AS `HarRestaurang`,
       `holidaymakerdb`.`boenden`.`AvstandStrand`  AS `AvstandStrand`,
       `holidaymakerdb`.`boenden`.`AvstandCentrum` AS `AvstandCentrum`,
       `holidaymakerdb`.`boenden`.`Omdome`         AS `Omdome`
from `holidaymakerdb`.`boenden`
where ((`holidaymakerdb`.`boenden`.`HarPool` = true) or (`holidaymakerdb`.`boenden`.`HarRestaurang` = true));

create or replace definer = root@localhost view vw_boendeintakter as
select `bo`.`BoendeID`                   AS `BoendeID`,
       `bo`.`Namn`                       AS `BoendeNamn`,
       `bo`.`Ort`                        AS `Ort`,
       count(`b`.`BokningsID`)           AS `TotalaBokningar`,
       coalesce(sum(`b`.`TotalPris`), 0) AS `TotalIntakt`
from ((`holidaymakerdb`.`boenden` `bo` left join `holidaymakerdb`.`rum` `r`
       on ((`bo`.`BoendeID` = `r`.`BoendeID`))) left join `holidaymakerdb`.`bokningar` `b`
      on ((`r`.`RumID` = `b`.`RumID`)))
group by `bo`.`BoendeID`, `bo`.`Namn`, `bo`.`Ort`;

create or replace definer = root@localhost view vw_kommandebokningar as
select `b`.`BokningsID`        AS `BokningsID`,
       `k`.`Namn`              AS `KundNamn`,
       `bo`.`Namn`             AS `BoendeNamn`,
       `r`.`Rumsnummer`        AS `Rumsnummer`,
       `b`.`IncheckningsDatum` AS `IncheckningsDatum`,
       `b`.`UtcheckningsDatum` AS `UtcheckningsDatum`,
       `b`.`AntalPersoner`     AS `AntalPersoner`
from (((`holidaymakerdb`.`bokningar` `b` join `holidaymakerdb`.`kunder` `k`
        on ((`b`.`KundID` = `k`.`KundID`))) join `holidaymakerdb`.`rum` `r`
       on ((`b`.`RumID` = `r`.`RumID`))) join `holidaymakerdb`.`boenden` `bo` on ((`r`.`BoendeID` = `bo`.`BoendeID`)))
where (`b`.`IncheckningsDatum` >= curdate())
order by `b`.`IncheckningsDatum`;

create or replace definer = root@localhost view vw_kundbokningar as
select `b`.`BokningsID`        AS `BokningsID`,
       `k`.`Namn`              AS `KundNamn`,
       `k`.`Email`             AS `Email`,
       `bo`.`Namn`             AS `BoendeNamn`,
       `bo`.`Ort`              AS `Ort`,
       `r`.`Rumsnummer`        AS `Rumsnummer`,
       `b`.`IncheckningsDatum` AS `IncheckningsDatum`,
       `b`.`UtcheckningsDatum` AS `UtcheckningsDatum`,
       `b`.`AntalPersoner`     AS `AntalPersoner`,
       `b`.`TotalPris`         AS `TotalPris`
from (((`holidaymakerdb`.`bokningar` `b` join `holidaymakerdb`.`kunder` `k`
        on ((`b`.`KundID` = `k`.`KundID`))) join `holidaymakerdb`.`rum` `r`
       on ((`b`.`RumID` = `r`.`RumID`))) join `holidaymakerdb`.`boenden` `bo` on ((`r`.`BoendeID` = `bo`.`BoendeID`)));

create or replace definer = root@localhost view vw_ortsstatistik as
select `b`.`Ort`                      AS `Ort`,
       count(distinct `b`.`BoendeID`) AS `AntalBoenden`,
       count(`r`.`RumID`)             AS `TotaltAntalRum`,
       min(`r`.`PrisPerNatt`)         AS `LägstaPris`
from (`holidaymakerdb`.`boenden` `b` left join `holidaymakerdb`.`rum` `r` on ((`b`.`BoendeID` = `r`.`BoendeID`)))
group by `b`.`Ort`;

create or replace definer = root@localhost view vw_rummedboende as
select `r`.`RumID`       AS `RumID`,
       `r`.`BoendeID`    AS `BoendeID`,
       `b`.`Namn`        AS `BoendeNamn`,
       `b`.`Ort`         AS `Ort`,
       `r`.`Rumsnummer`  AS `Rumsnummer`,
       `r`.`Kapacitet`   AS `Kapacitet`,
       `r`.`PrisPerNatt` AS `PrisPerNatt`
from (`holidaymakerdb`.`rum` `r` join `holidaymakerdb`.`boenden` `b` on ((`r`.`BoendeID` = `b`.`BoendeID`)));

