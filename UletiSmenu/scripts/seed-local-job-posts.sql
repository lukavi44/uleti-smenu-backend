-- Seed active dummy job posts for local development (UletiSmenuDb).
-- Uses relative future dates so posts stay visible after re-runs.
-- Safe to re-run: only inserts when fewer than 5 visible posts exist.

SET NOCOUNT ON;

DECLARE @nowUtc datetime2 = SYSUTCDATETIME();
DECLARE @visibleCount int;

SELECT @visibleCount = COUNT(*)
FROM JobPosts
WHERE Status = 'Active'
  AND (VisibleUntil >= @nowUtc OR DATEADD(hour, 1, StartingDate) >= @nowUtc);

IF @visibleCount >= 5
BEGIN
    PRINT 'Skipping seed: enough visible job posts already exist.';
    RETURN;
END

DECLARE @loftEmployer uniqueidentifier = '31B309CE-34B4-4818-95EC-A1E7995ABFE9';
DECLARE @pivnicaEmployer uniqueidentifier = '941F57C8-B7D5-4C14-945A-30970EDE08B8';
DECLARE @loftPromenada uniqueidentifier = 'DDFFE852-CC1F-48A4-9FE3-EE3A933DA59D';
DECLARE @loftCoffee uniqueidentifier = '61D65D81-4D95-45DF-BAE9-FE157EA13740';
DECLARE @gradskaMain uniqueidentifier = '88A858E8-7256-43F8-9C81-317522B3DF3D';
DECLARE @novaPivnica uniqueidentifier = '57665D07-3D2E-4B0C-A2FC-A35E97E3CBCC';

INSERT INTO JobPosts (
    Id, Title, Description, Position, Status, Salary,
    CreatedAtUtc, StartingDate, VisibleUntil, EmployerId, RestaurantLocationId
)
VALUES
    (NEWID(), N'Konobar vikend smena - Loft Promenada', N'Potreban iskusan konobar za vikend smenu. Nega gostiju, POS i brza usluga.', N'Konobar', 'Active', 4200, @nowUtc, DATEADD(day, 2, DATEADD(hour, 17, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 2, DATEADD(hour, 17, CAST(CAST(@nowUtc AS date) AS datetime2))), @loftEmployer, @loftPromenada),
    (NEWID(), N'Sanker petak vece', N'Rad na sanku, pripremanje koktela i osnovnih pica. Timski rad.', N'Sanker', 'Active', 3800, @nowUtc, DATEADD(day, 3, DATEADD(hour, 18, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 3, DATEADD(hour, 18, CAST(CAST(@nowUtc AS date) AS datetime2))), @loftEmployer, @loftPromenada),
    (NEWID(), N'Barista jutarnja smena', N'Priprema kafe i osnovnih napataka. Pocetak u 07:00.', N'Barista', 'Active', 3200, @nowUtc, DATEADD(day, 4, DATEADD(hour, 7, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 4, DATEADD(hour, 7, CAST(CAST(@nowUtc AS date) AS datetime2))), @loftEmployer, @loftCoffee),
    (NEWID(), N'Hostesa subota', N'Dobrodoslica gostima, raspored stolova i koordinacija sa salom.', N'Hostesa', 'Active', 3500, @nowUtc, DATEADD(day, 5, DATEADD(hour, 12, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 5, DATEADD(hour, 12, CAST(CAST(@nowUtc AS date) AS datetime2))), @loftEmployer, @loftPromenada),
    (NEWID(), N'Kuvar pomocna kuhinja', N'Priprema sastojaka, podrska glavnom kuharu, odrzavanje higijene.', N'Kuvar', 'Active', 4500, @nowUtc, DATEADD(day, 6, DATEADD(hour, 16, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 6, DATEADD(hour, 16, CAST(CAST(@nowUtc AS date) AS datetime2))), @loftEmployer, @loftCoffee),
    (NEWID(), N'Konobar sreda popodne', N'Pokrivanje popodnevne smene u coffee baru.', N'Konobar', 'Active', 3600, @nowUtc, DATEADD(day, 1, DATEADD(hour, 14, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 1, DATEADD(hour, 14, CAST(CAST(@nowUtc AS date) AS datetime2))), @loftEmployer, @loftCoffee),
    (NEWID(), N'Sef smene nedelja', N'Koordinacija tima, otvaranje i zatvaranje lokacije.', N'Sef smene', 'Active', 5200, @nowUtc, DATEADD(day, 7, DATEADD(hour, 10, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 7, DATEADD(hour, 10, CAST(CAST(@nowUtc AS date) AS datetime2))), @loftEmployer, @loftPromenada),
    (NEWID(), N'Perac/raner vecer', N'Brza rotacija posudja i podrska kuhinji tokom guzve.', N'Perac/Raner', 'Active', 3000, @nowUtc, DATEADD(day, 8, DATEADD(hour, 19, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 8, DATEADD(hour, 19, CAST(CAST(@nowUtc AS date) AS datetime2))), @loftEmployer, @loftCoffee),

    (NEWID(), N'Konobar - Gradska pivnica', N'Potreban konobar za salu. Iskustvo u ugostiteljstvu je plus.', N'Konobar', 'Active', 4000, @nowUtc, DATEADD(day, 2, DATEADD(hour, 18, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 2, DATEADD(hour, 18, CAST(CAST(@nowUtc AS date) AS datetime2))), @pivnicaEmployer, @gradskaMain),
    (NEWID(), N'Sanker subota', N'Rad na sanku, servis pica i koktela.', N'Sanker', 'Active', 3900, @nowUtc, DATEADD(day, 4, DATEADD(hour, 20, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 4, DATEADD(hour, 20, CAST(CAST(@nowUtc AS date) AS datetime2))), @pivnicaEmployer, @gradskaMain),
    (NEWID(), N'Kuvar rostilj', N'Priprema jela sa rostilja. Puno radno vreme jedne smene.', N'Kuvar', 'Active', 4800, @nowUtc, DATEADD(day, 7, DATEADD(hour, 15, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 7, DATEADD(hour, 15, CAST(CAST(@nowUtc AS date) AS datetime2))), @pivnicaEmployer, @novaPivnica),
    (NEWID(), N'Konobar Nova pivnica', N'Pokrivanje smene u novoj filijali. Obuka na licu mesta.', N'Konobar', 'Active', 3700, @nowUtc, DATEADD(day, 3, DATEADD(hour, 17, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 3, DATEADD(hour, 17, CAST(CAST(@nowUtc AS date) AS datetime2))), @pivnicaEmployer, @novaPivnica),
    (NEWID(), N'Hostesa petak', N'Rezervacije, raspored i dobrodoslica gostima.', N'Hostesa', 'Active', 3400, @nowUtc, DATEADD(day, 3, DATEADD(hour, 19, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 3, DATEADD(hour, 19, CAST(CAST(@nowUtc AS date) AS datetime2))), @pivnicaEmployer, @gradskaMain),
    (NEWID(), N'Perac vikend', N'Podrska kuhinji tokom vikend guzve.', N'Perac/Raner', 'Active', 3100, @nowUtc, DATEADD(day, 5, DATEADD(hour, 18, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 5, DATEADD(hour, 18, CAST(CAST(@nowUtc AS date) AS datetime2))), @pivnicaEmployer, @novaPivnica),
    (NEWID(), N'Barista jutro', N'Priprema kafe i dorucnih napataka.', N'Barista', 'Active', 3300, @nowUtc, DATEADD(day, 6, DATEADD(hour, 8, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 6, DATEADD(hour, 8, CAST(CAST(@nowUtc AS date) AS datetime2))), @pivnicaEmployer, @gradskaMain),
    (NEWID(), N'Konobar utorak vece', N'Smena u centru grada, tim od 4 osobe.', N'Konobar', 'Active', 4100, @nowUtc, DATEADD(day, 7, DATEADD(hour, 18, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 7, DATEADD(hour, 18, CAST(CAST(@nowUtc AS date) AS datetime2))), @pivnicaEmployer, @gradskaMain),
    (NEWID(), N'Sanker nedelja', N'Potreban sanker za nedeljnu smenu.', N'Sanker', 'Active', 3850, @nowUtc, DATEADD(day, 6, DATEADD(hour, 19, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 6, DATEADD(hour, 19, CAST(CAST(@nowUtc AS date) AS datetime2))), @pivnicaEmployer, @novaPivnica),
    (NEWID(), N'Kuvar ponedeljak', N'Pomoc u kuhinji, iskustvo sa brzom uslugom.', N'Kuvar', 'Active', 4400, @nowUtc, DATEADD(day, 8, DATEADD(hour, 16, CAST(CAST(@nowUtc AS date) AS datetime2))), DATEADD(day, 8, DATEADD(hour, 16, CAST(CAST(@nowUtc AS date) AS datetime2))), @pivnicaEmployer, @gradskaMain);

SELECT COUNT(*) AS VisiblePosts
FROM JobPosts
WHERE Status = 'Active'
  AND (VisibleUntil >= @nowUtc OR DATEADD(hour, 1, StartingDate) >= @nowUtc);
