-- ============================================================
-- RaceDay Database Schema
-- SQL Server (SSMS) - Run on a clean instance
-- Part 2 note: EF Core Code-First (src/RaceDay.Api/Data) generates this same schema.
-- Matches ERD exactly: 7 entities, all PKs, FKs, constraints
-- ============================================================

-- Drop tables in reverse dependency order if they exist
IF OBJECT_ID('dbo.EventAnnouncements', 'U') IS NOT NULL DROP TABLE dbo.EventAnnouncements;
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Routes', 'U') IS NOT NULL DROP TABLE dbo.Routes;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;

-- ============================================================
-- 1. USERS
-- ============================================================
CREATE TABLE Users (
    user_id         INT             IDENTITY(1,1)   PRIMARY KEY,
    email           VARCHAR(255)    NOT NULL         UNIQUE,
    password_hash   VARCHAR(255)    NOT NULL,
    first_name      VARCHAR(100)    NOT NULL,
    last_name       VARCHAR(100)    NOT NULL,
    role            VARCHAR(20)     NOT NULL         CHECK (role IN ('Organiser', 'Participant')),
    phone           VARCHAR(20)     NULL,
    date_of_birth   DATE            NULL,
    gender          VARCHAR(10)     NULL             CHECK (gender IN ('Male', 'Female', 'Other') OR gender IS NULL),
    profile_picture_url VARCHAR(1000) NULL,         -- Part 3: Azure Blob Storage URL
    province        VARCHAR(50)     NULL,
    created_at      DATETIME        NOT NULL         DEFAULT GETDATE(),
    updated_at      DATETIME        NOT NULL         DEFAULT GETDATE()
);

-- ============================================================
-- 2. EVENTS
-- ============================================================
CREATE TABLE Events (
    event_id        INT             IDENTITY(1,1)   PRIMARY KEY,
    organiser_id    INT             NOT NULL,
    title           VARCHAR(255)    NOT NULL,
    description     TEXT            NULL,
    distance_km     DECIMAL(5,2)    NULL,            -- Part 3: headline distance shown on the event
    event_date      DATE            NOT NULL,
    location        VARCHAR(255)    NOT NULL,
    province        VARCHAR(50)     NULL,
    event_type      VARCHAR(20)     NOT NULL         CHECK (event_type IN ('Running', 'Walking', 'Cycling')),
    status          VARCHAR(20)     NOT NULL         DEFAULT 'Draft'
                                                     CHECK (status IN ('Draft', 'Published', 'Completed', 'Cancelled')),
    contact_email   VARCHAR(255)    NULL,
    image_url       VARCHAR(500)    NULL,            -- Part 3: banner URL in Azure Blob Storage
    created_at      DATETIME        NOT NULL         DEFAULT GETDATE(),
    updated_at      DATETIME        NOT NULL         DEFAULT GETDATE(),

    CONSTRAINT FK_Events_Organiser FOREIGN KEY (organiser_id) REFERENCES Users(user_id)
);

-- ============================================================
-- 3. CATEGORIES
-- ============================================================
CREATE TABLE Categories (
    category_id         INT             IDENTITY(1,1)   PRIMARY KEY,
    event_id            INT             NOT NULL,
    name                VARCHAR(100)    NOT NULL,
    distance_km         DECIMAL(5,2)    NOT NULL,
    entry_fee           DECIMAL(10,2)   NOT NULL        DEFAULT 0.00,
    max_participants    INT             NULL,
    min_age             INT             NULL,
    max_age             INT             NULL,
    gender_restriction  VARCHAR(10)     NULL            CHECK (gender_restriction IN ('Male', 'Female') OR gender_restriction IS NULL),

    CONSTRAINT FK_Categories_Event FOREIGN KEY (event_id) REFERENCES Events(event_id) ON DELETE CASCADE
);

-- ============================================================
-- 4. ENROLMENTS
-- ============================================================
CREATE TABLE Enrolments (
    enrolment_id            INT             IDENTITY(1,1)   PRIMARY KEY,
    participant_id          INT             NOT NULL,
    category_id             INT             NOT NULL,
    enrolment_date          DATETIME        NOT NULL        DEFAULT GETDATE(),
    bib_number              VARCHAR(20)     NULL,
    status                  VARCHAR(20)     NOT NULL        DEFAULT 'Pending'
                                                            CHECK (status IN ('Pending', 'Confirmed', 'Cancelled')),
    emergency_contact_name  VARCHAR(100)    NULL,
    emergency_contact_phone VARCHAR(20)     NULL,

    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (participant_id) REFERENCES Users(user_id),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (category_id) REFERENCES Categories(category_id) ON DELETE CASCADE,
    CONSTRAINT UQ_Enrolment_Unique UNIQUE (participant_id, category_id)
);

-- ============================================================
-- 5. RESULTS
-- ============================================================
CREATE TABLE Results (
    result_id       INT             IDENTITY(1,1)   PRIMARY KEY,
    enrolment_id    INT             NOT NULL        UNIQUE,
    finish_time     TIME            NULL,
    position        INT             NULL,
    status          VARCHAR(20)     NOT NULL        CHECK (status IN ('Finished', 'DNF', 'DNS', 'DQ')),
    recorded_by     INT             NULL,
    recorded_at     DATETIME        NOT NULL        DEFAULT GETDATE(),

    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (enrolment_id) REFERENCES Enrolments(enrolment_id) ON DELETE CASCADE,
    CONSTRAINT FK_Results_RecordedBy FOREIGN KEY (recorded_by) REFERENCES Users(user_id)
);

-- ============================================================
-- 6. ROUTES
-- ============================================================
CREATE TABLE Routes (
    route_id            INT             IDENTITY(1,1)   PRIMARY KEY,
    event_id            INT             NOT NULL        UNIQUE,
    start_latitude      DECIMAL(9,6)    NULL,
    start_longitude     DECIMAL(9,6)    NULL,
    end_latitude        DECIMAL(9,6)    NULL,
    end_longitude       DECIMAL(9,6)    NULL,
    description         TEXT            NULL,
    elevation_gain_m    INT             NULL,
    terrain_type        VARCHAR(50)     NULL,

    CONSTRAINT FK_Routes_Event FOREIGN KEY (event_id) REFERENCES Events(event_id) ON DELETE CASCADE
);

-- ============================================================
-- 7. EVENT ANNOUNCEMENTS
-- ============================================================
CREATE TABLE EventAnnouncements (
    announcement_id INT             IDENTITY(1,1)   PRIMARY KEY,
    event_id        INT             NOT NULL,
    author_id       INT             NOT NULL,
    title           VARCHAR(255)    NOT NULL,
    message         TEXT            NOT NULL,
    priority        VARCHAR(20)     NOT NULL        DEFAULT 'Normal'
                                                    CHECK (priority IN ('Low', 'Normal', 'High', 'Urgent')),
    created_at      DATETIME        NOT NULL        DEFAULT GETDATE(),

    CONSTRAINT FK_Announcements_Event FOREIGN KEY (event_id) REFERENCES Events(event_id) ON DELETE CASCADE,
    CONSTRAINT FK_Announcements_Author FOREIGN KEY (author_id) REFERENCES Users(user_id)
);


-- ============================================================
-- SEED DATA
-- ============================================================

-- -- USERS --
-- Every seeded account uses the password 'Password123!'.
-- The value is an ASP.NET Core Identity PasswordHasher hash (PBKDF2, salted), the same
-- format the Part 2 API uses, so these accounts can log in through the API.
SET IDENTITY_INSERT Users ON;

INSERT INTO Users (user_id, email, password_hash, first_name, last_name, role, phone, date_of_birth, gender, province)
VALUES
    -- Organisers (2)
    (1, 'thabo.mokoena@raceday.co.za', 'AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==', 'Thabo', 'Mokoena', 'Organiser', '082 555 0101', '1985-03-15', 'Male', 'Gauteng'),
    (2, 'lerato.dlamini@raceday.co.za', 'AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==', 'Lerato', 'Dlamini', 'Organiser', '083 555 0202', '1990-07-22', 'Female', 'Western Cape'),

    -- Participants (4)
    (3, 'sipho.ndlovu@gmail.com', 'AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==', 'Sipho', 'Ndlovu', 'Participant', '071 555 0303', '1995-11-08', 'Male', 'KwaZulu-Natal'),
    (4, 'naledi.khumalo@gmail.com', 'AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==', 'Naledi', 'Khumalo', 'Participant', '072 555 0404', '1998-02-14', 'Female', 'Gauteng'),
    (5, 'james.venter@outlook.com', 'AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==', 'James', 'Venter', 'Participant', '082 555 0505', '1988-09-30', 'Male', 'Western Cape'),
    (6, 'amahle.zulu@yahoo.com', 'AQAAAAIAAYagAAAAEM3yVBdri1JnLOwPQYTjOeQevMHqGYNhUYdxMp9joXYol06nEC2EY4Oj8Ot0Fl7yOQ==', 'Amahle', 'Zulu', 'Participant', '061 555 0606', '2000-06-18', 'Female', 'Eastern Cape');

SET IDENTITY_INSERT Users OFF;


-- -- EVENTS --
SET IDENTITY_INSERT Events ON;

INSERT INTO Events (event_id, organiser_id, title, description, event_date, location, province, event_type, status, contact_email)
VALUES
    (1, 1, 'Joburg City Marathon 2026', 'A flagship road running event through the heart of Johannesburg, starting at the FNB Stadium and finishing at Sandton City.', '2026-09-20', 'FNB Stadium, Johannesburg', 'Gauteng', 'Running', 'Published', 'info@joburgmarathon.co.za'),
    (2, 2, 'Cape Town Coastal Cycle', 'A scenic cycling event along the Atlantic Seaboard from the V&A Waterfront to Hout Bay and back.', '2026-10-15', 'V&A Waterfront, Cape Town', 'Western Cape', 'Cycling', 'Published', 'events@capecoastalcycle.co.za'),
    (3, 1, 'Soweto Heritage Walk', 'A community walking event celebrating Soweto heritage, passing through Vilakazi Street and Freedom Square.', '2026-11-01', 'Vilakazi Street, Soweto', 'Gauteng', 'Walking', 'Published', 'walk@sowetoheritage.org.za');

SET IDENTITY_INSERT Events OFF;


-- -- CATEGORIES --
SET IDENTITY_INSERT Categories ON;

INSERT INTO Categories (category_id, event_id, name, distance_km, entry_fee, max_participants, min_age, max_age, gender_restriction)
VALUES
    -- Joburg City Marathon (Event 1)
    (1, 1, '42.2km Full Marathon', 42.20, 350.00, 5000, 18, NULL, NULL),
    (2, 1, '21.1km Half Marathon', 21.10, 250.00, 8000, 16, NULL, NULL),
    (3, 1, '10km Fun Run', 10.00, 120.00, 10000, 12, NULL, NULL),

    -- Cape Town Coastal Cycle (Event 2)
    (4, 2, '80km Full Route', 80.00, 450.00, 3000, 18, NULL, NULL),
    (5, 2, '40km Half Route', 40.00, 300.00, 5000, 16, NULL, NULL),

    -- Soweto Heritage Walk (Event 3)
    (6, 3, '10km Heritage Trail', 10.00, 50.00, 2000, NULL, NULL, NULL),
    (7, 3, '5km Family Walk', 5.00, 0.00, 5000, NULL, NULL, NULL);

SET IDENTITY_INSERT Categories OFF;


-- -- ROUTES --
SET IDENTITY_INSERT Routes ON;

INSERT INTO Routes (route_id, event_id, start_latitude, start_longitude, end_latitude, end_longitude, description, elevation_gain_m, terrain_type)
VALUES
    (1, 1, -26.235900, 27.982500, -26.107300, 28.056700, 'Start at FNB Stadium, head north through Braamfontein, past the Johannesburg CBD, through Rosebank, and finish at Sandton City. Mostly tar roads with gentle elevation changes.', 185, 'Road'),
    (2, 2, -33.903200, 18.420700, -34.043900, 18.353800, 'Start at V&A Waterfront, ride along the Atlantic Seaboard via Sea Point, Camps Bay, and Llandudno to Hout Bay. Return via Constantia Nek. Rolling coastal terrain.', 640, 'Road'),
    (3, 3, -26.336700, 27.896200, -26.341500, 27.905400, 'Start at Vilakazi Street, loop through Orlando West, past the Hector Pieterson Museum, through Freedom Square, and return. Flat urban terrain.', 25, 'Road');

SET IDENTITY_INSERT Routes OFF;


-- -- ENROLMENTS --
SET IDENTITY_INSERT Enrolments ON;

INSERT INTO Enrolments (enrolment_id, participant_id, category_id, enrolment_date, bib_number, status, emergency_contact_name, emergency_contact_phone)
VALUES
    -- Sipho enters Joburg Marathon (Half)
    (1, 3, 2, '2026-07-01 10:30:00', 'JBM-2145', 'Confirmed', 'Nomsa Ndlovu', '073 111 2233'),
    -- Naledi enters Joburg Marathon (10km)
    (2, 4, 3, '2026-07-05 14:15:00', 'JBM-5032', 'Confirmed', 'David Khumalo', '082 444 5566'),
    -- James enters Cape Town Cycle (Full)
    (3, 5, 4, '2026-08-10 09:00:00', 'CCC-0789', 'Confirmed', 'Sarah Venter', '083 777 8899'),
    -- Amahle enters Joburg Marathon (Half)
    (4, 6, 2, '2026-07-12 16:45:00', 'JBM-2301', 'Confirmed', 'Bongani Zulu', '071 222 3344'),
    -- Sipho also enters Soweto Walk
    (5, 3, 6, '2026-08-20 11:00:00', NULL, 'Pending', 'Nomsa Ndlovu', '073 111 2233'),
    -- Naledi enters Cape Town Cycle (Half)
    (6, 4, 5, '2026-08-15 08:30:00', 'CCC-1456', 'Confirmed', 'David Khumalo', '082 444 5566'),
    -- James enters Soweto Walk
    (7, 5, 7, '2026-08-25 12:00:00', NULL, 'Pending', 'Sarah Venter', '083 777 8899');

SET IDENTITY_INSERT Enrolments OFF;


-- -- RESULTS --
-- Note: Only for events that have already been "completed" in a realistic scenario.
-- Results are added for the Joburg Marathon as if it has already taken place.
SET IDENTITY_INSERT Results ON;

INSERT INTO Results (result_id, enrolment_id, finish_time, position, status, recorded_by, recorded_at)
VALUES
    -- Half Marathon results (Enrolments 1 and 4)
    (1, 1, '01:42:35', 1, 'Finished', 1, '2026-09-20 12:30:00'),
    (2, 4, '02:05:18', 2, 'Finished', 1, '2026-09-20 12:45:00'),
    -- 10km Fun Run result (Enrolment 2)
    (3, 2, '00:52:10', 1, 'Finished', 1, '2026-09-20 11:15:00');

SET IDENTITY_INSERT Results OFF;


-- -- EVENT ANNOUNCEMENTS --
SET IDENTITY_INSERT EventAnnouncements ON;

INSERT INTO EventAnnouncements (announcement_id, event_id, author_id, title, message, priority, created_at)
VALUES
    (1, 1, 1, 'Route Change Notice', 'Due to road works on Jan Smuts Avenue, the 42.2km route will be diverted via Oxford Road between km 18 and km 22. Updated maps are available on the event page.', 'High', '2026-09-10 09:00:00'),
    (2, 1, 1, 'Race Pack Collection', 'Race packs can be collected from FNB Stadium on 18-19 September between 09:00 and 17:00. Please bring your ID and confirmation email.', 'Normal', '2026-09-12 14:00:00'),
    (3, 2, 2, 'Registration Closing Soon', 'Online registration for the Cape Town Coastal Cycle closes on 10 October. Late entries will be accepted at the venue for an additional R100.', 'Normal', '2026-09-28 10:00:00'),
    (4, 3, 1, 'Event Confirmed', 'The Soweto Heritage Walk is confirmed for 1 November 2026. Water stations will be placed every 2km along the route.', 'Low', '2026-10-01 08:00:00');

SET IDENTITY_INSERT EventAnnouncements OFF;


-- ============================================================
-- VERIFY: Quick check that all tables and data are present
-- ============================================================
SELECT 'Users' AS [Table], COUNT(*) AS [Rows] FROM Users
UNION ALL SELECT 'Events', COUNT(*) FROM Events
UNION ALL SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL SELECT 'Enrolments', COUNT(*) FROM Enrolments
UNION ALL SELECT 'Results', COUNT(*) FROM Results
UNION ALL SELECT 'Routes', COUNT(*) FROM Routes
UNION ALL SELECT 'EventAnnouncements', COUNT(*) FROM EventAnnouncements;
