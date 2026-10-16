# RaceDay Event Management System

## Project Overview

RaceDay is an ASP.NET Core Web API developed for managing sporting
events such as running, walking and cycling events.

The system supports two primary user roles:

- Organiser
- Participant

Organisers can create and manage events, define event categories,
view event enrolments and record race results.

Participants can register, manage their profile, browse events,
enrol in event categories and view their results.

## Technologies

- C#
- ASP.NET Core Web API
- .NET 8
- Entity Framework Core 8
- SQL Server
- SQL Server Management Studio
- Swagger / OpenAPI
- xUnit
- EF Core InMemory
- Git
- GitHub
- GitHub Actions

## Main Features

### Authentication
- Participant registration
- Login and logout
- Cookie-based authentication
- Role-based authorization
- Secure password hashing

### Profile Management
- View authenticated profile
- Update authenticated profile

### Event Management
- View events
- View individual event
- Organiser event creation
- Organiser event updates
- Organiser event deletion
- Ownership protection

### Categories
- View event categories
- Organiser category creation
- Duplicate category prevention
- Category deletion
- Enrolment protection

### Enrolments
- Participant event enrolment
- Duplicate enrolment prevention
- Participant enrolment history
- Organiser viewing of event enrolments
- Enrolment cancellation

### Results
- Organiser result recording
- Participant result viewing
- Organiser event-result viewing
- Event ownership protection

## Database

The application uses SQL Server and Entity Framework Core Code First.

Development database:

RaceDayDB_Part2

The database schema contains:

- Roles
- Users
- Events
- Categories
- Enrolments
- Results

EF Core migrations are used to create and maintain the Part 2
development database.

## Automated Testing

Automated tests are implemented using xUnit and EF Core InMemory.

The test suite covers Events, Categories, Enrolments and Results,
including successful and failure scenarios.

Current local test result:

6 Passed
0 Failed

## Continuous Integration

GitHub Actions automatically performs:

1. Repository checkout
2. .NET 8 setup
3. NuGet restore
4. Solution build
5. Automated tests

The CI workflow runs when changes are pushed to the repository
and on pull requests.

## Running the Project

1. Clone the repository.
2. Open RaceDay.slnx in Visual Studio.
3. Restore NuGet packages.
4. Configure RaceDayConnection in appsettings.json.
5. Apply the Entity Framework Core migrations.
6. Build the solution.
7. Run RaceDay.API.
8. Open Swagger in the browser.
9. Register/login and test the API endpoints.

## Security

Public registration creates Participant accounts.

Organiser privileges should be assigned through a trusted
administrative process rather than allowing users to grant
themselves the Organiser role.

Protected API endpoints use role-based authorisation and ownership
checks.

Passwords are stored using ASP.NET Core password hashing rather
than plain text.

## CI/CD Status

The repository contains more than 20 meaningful Git commits.

GitHub Actions currently completes the RaceDay build and automated
test workflow successfully.

## Video Demonstration

https://youtu.be/DIlPPcNDX2s

## Author

Erwin Mashobane
Student Number: ST10073464
Programming 2B
PROG6212
