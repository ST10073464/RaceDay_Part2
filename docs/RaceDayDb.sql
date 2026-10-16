/*
    RaceDay - Part 1 SQL Database Script
    Module: Programming 2B (PROG6212)
    Student: Erwin Mashobane
    Student Number: ST10073464

    The schema is designed to match the Part 1 ERD exactly.
    SQL Server / SSMS
*/

IF DB_ID('RaceDayDb') IS NULL
BEGIN
    CREATE DATABASE RaceDayDb;
END
GO

USE RaceDayDb;
GO

/* Clean existing tables so the script can be tested repeatedly. */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

CREATE TABLE dbo.Roles
(
    RoleId INT IDENTITY(1,1) NOT NULL,
    RoleName NVARCHAR(30) NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName),
    CONSTRAINT CK_Roles_RoleName CHECK (RoleName IN ('Organiser', 'Participant'))
);
GO

CREATE TABLE dbo.Users
(
    UserId INT IDENTITY(1,1) NOT NULL,
    RoleId INT NOT NULL,
    FirstName NVARCHAR(80) NOT NULL,
    LastName NVARCHAR(80) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    PhoneNumber NVARCHAR(30) NULL,
    DateOfBirth DATE NULL,
    ProfilePictureUrl NVARCHAR(1000) NULL,
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.Events
(
    EventId INT IDENTITY(1,1) NOT NULL,
    OrganiserId INT NOT NULL,
    EventName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(1000) NULL,
    EventDate DATE NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    EventType NVARCHAR(20) NOT NULL,
    BannerImageUrl NVARCHAR(1000) NULL,
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Events_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Events_Distance CHECK (DistanceKm > 0),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle'))
);
GO

CREATE TABLE dbo.Categories
(
    CategoryId INT IDENTITY(1,1) NOT NULL,
    EventId INT NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    MinAge INT NULL,
    MaxAge INT NULL,
    DistanceKm DECIMAL(6,2) NULL,

    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),
    CONSTRAINT UQ_Categories_Event_Category UNIQUE (EventId, CategoryName),
    CONSTRAINT CK_Categories_AgeRange CHECK
        (MinAge IS NULL OR MaxAge IS NULL OR MinAge <= MaxAge),
    CONSTRAINT CK_Categories_MinAge CHECK (MinAge IS NULL OR MinAge >= 0),
    CONSTRAINT CK_Categories_MaxAge CHECK (MaxAge IS NULL OR MaxAge >= 0),
    CONSTRAINT CK_Categories_Distance CHECK (DistanceKm IS NULL OR DistanceKm > 0)
);
GO

CREATE TABLE dbo.Enrolments
(
    EnrolmentId INT IDENTITY(1,1) NOT NULL,
    ParticipantId INT NOT NULL,
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentDate DATETIME2(0) NOT NULL
        CONSTRAINT DF_Enrolments_EnrolmentDate DEFAULT SYSUTCDATETIME(),
    EnrolmentStatus NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Enrolments_Status DEFAULT 'Confirmed',

    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Enrolments_Event FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantId, EventId),
    CONSTRAINT CK_Enrolments_Status CHECK
        (EnrolmentStatus IN ('Pending', 'Confirmed', 'Cancelled'))
);
GO

CREATE TABLE dbo.Results
(
    ResultId INT IDENTITY(1,1) NOT NULL,
    EnrolmentId INT NOT NULL,
    FinishTime TIME(0) NOT NULL,
    FinishingPosition INT NOT NULL,
    PublishedAt DATETIME2(0) NULL,

    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES dbo.Enrolments(EnrolmentId),
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentId),
    CONSTRAINT CK_Results_Position CHECK (FinishingPosition > 0)
);
GO

/* Seed roles. */
INSERT INTO dbo.Roles (RoleName)
VALUES ('Organiser'), ('Participant');
GO

/*
    PasswordHash values are deliberately sample placeholders for Part 1.
    Part 2 must hash real passwords before storage.
*/
INSERT INTO dbo.Users
    (RoleId, FirstName, LastName, Email, PasswordHash, PhoneNumber, DateOfBirth)
VALUES
    (1, 'Lerato', 'Mokoena', 'lerato.organiser@raceday.example',
        'PART1_SAMPLE_HASH_ORGANISER_1', '0825551001', '1985-03-12'),
    (1, 'Thabo', 'Dlamini', 'thabo.organiser@raceday.example',
        'PART1_SAMPLE_HASH_ORGANISER_2', '0825551002', '1982-07-25'),
    (2, 'Naledi', 'Molefe', 'naledi.participant@raceday.example',
        'PART1_SAMPLE_HASH_PARTICIPANT_1', '0825552001', '2001-05-14'),
    (2, 'Sipho', 'Nkosi', 'sipho.participant@raceday.example',
        'PART1_SAMPLE_HASH_PARTICIPANT_2', '0825552002', '1998-11-03');
GO

INSERT INTO dbo.Events
    (OrganiserId, EventName, Description, EventDate, Location, DistanceKm, EventType)
VALUES
    (1, 'Pretoria Spring Run', 'Community road running event in Pretoria.',
        '2026-09-20', 'Pretoria, Gauteng', 10.00, 'Run'),
    (1, 'Soweto Community Walk', 'Community walking event supporting active lifestyles.',
        '2026-10-04', 'Soweto, Gauteng', 8.00, 'Walk'),
    (2, 'Cape Cycle Challenge', 'Road cycling event for recreational and competitive cyclists.',
        '2026-10-18', 'Cape Town, Western Cape', 42.00, 'Cycle');
GO

INSERT INTO dbo.Categories
    (EventId, CategoryName, MinAge, MaxAge, DistanceKm)
VALUES
    (1, 'Under 20 - 10km', 0, 19, 10.00),
    (1, 'Senior - 10km', 20, NULL, 10.00),
    (2, 'Open Walk - 8km', 16, NULL, 8.00),
    (2, 'Junior Walk - 8km', 0, 15, 8.00),
    (3, 'Open Cycle - 42km', 18, NULL, 42.00),
    (3, 'Junior Cycle - 42km', 16, 17, 42.00);
GO

INSERT INTO dbo.Enrolments
    (ParticipantId, EventId, CategoryId, EnrolmentStatus)
VALUES
    (3, 1, 2, 'Confirmed'),
    (4, 1, 2, 'Confirmed'),
    (3, 2, 3, 'Confirmed'),
    (4, 3, 5, 'Confirmed');
GO

INSERT INTO dbo.Results
    (EnrolmentId, FinishTime, FinishingPosition, PublishedAt)
VALUES
    (1, '00:52:14', 47, SYSUTCDATETIME()),
    (2, '00:55:31', 62, SYSUTCDATETIME());
GO

/* Verification queries for SSMS demonstration. */
SELECT * FROM dbo.Roles;
SELECT * FROM dbo.Users;
SELECT * FROM dbo.Events;
SELECT * FROM dbo.Categories;
SELECT * FROM dbo.Enrolments;
SELECT * FROM dbo.Results;
GO
