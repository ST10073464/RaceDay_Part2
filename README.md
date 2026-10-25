# RaceDay Event Management System

## Project Overview

RaceDay is a full-stack ASP.NET Core event management system developed for managing sporting events such as running, walking and cycling events.

The solution consists of an ASP.NET Core Web API backend and an ASP.NET Core MVC frontend. The MVC application communicates with the API for authentication, event management, enrolments, results, profile management and image management.

The system supports two primary user roles:

- Organiser
- Participant

Organisers can create and manage events, manage event categories, view event participants, record race results and manage event banner images.

Participants can register and log in, browse upcoming events, enrol in event categories, view their enrolments and race results, update their personal information and manage their profile picture.

---

## Solution Structure

The RaceDay solution contains four projects:

### RaceDay.API

ASP.NET Core Web API responsible for:

- Authentication and authorisation
- Database access
- User profiles
- Event management
- Category management
- Enrolments
- Results
- Azure Blob Storage integration

### RaceDay.Mvc

ASP.NET Core MVC frontend responsible for:

- User registration and login
- Participant interface
- Organiser interface
- Role-aware navigation
- Profile management interface
- Event management interface
- Enrolment and results interfaces
- Communicating with RaceDay.API through HttpClient

The MVC application does not access the RaceDay database or Azure Blob Storage directly.

### RaceDay.Contracts

Contains shared request and response contracts used by the RaceDay projects.

### RaceDay.Api.Tests

Contains automated tests for the RaceDay API.

---

## Technologies

- C#
- .NET 8
- ASP.NET Core Web API
- ASP.NET Core MVC
- Entity Framework Core 8
- SQL Server
- SQL Server Management Studio
- Bootstrap
- HTML
- CSS
- JavaScript
- Azure Blob Storage
- Swagger / OpenAPI
- Docker
- Docker Desktop
- xUnit
- EF Core InMemory
- Git
- GitHub
- GitHub Actions

---

## Main Features

### Authentication

- Participant registration
- Login and logout
- Cookie-based API authentication
- MVC session management
- Role-based authorisation
- Role-aware navigation
- Secure password hashing
- Protected Organiser and Participant functionality

### Profile Management

Participants and Organisers can:

- View their authenticated profile
- Update personal information
- Upload a profile picture
- Replace an existing profile picture
- Remove a profile picture

Profile pictures are stored using Azure Blob Storage.

The profile Save Changes button remains disabled until personal information is actually modified.

Image upload buttons remain disabled until a file is selected.

### Event Management

Organisers can:

- View their events
- Create events
- Edit events
- Delete events
- Manage event categories
- View event participants
- Record and view race results
- Upload event banners
- Replace event banners
- Remove event banners

Event information includes:

- Event name
- Description
- Event date
- Location
- Distance
- Event type
- Event banner

Ownership protection prevents Organisers from modifying events that they do not own.

The Save Changes button remains disabled until event information is actually modified.

A confirmation prompt is displayed before profile or event changes are submitted to help prevent accidental modifications.

### Categories

- View event categories
- Organiser category creation
- Duplicate category prevention
- Category deletion
- Enrolment protection

### Enrolments

Participants can:

- Browse upcoming events
- View event details
- Select an event category
- Enrol in an event
- View My Enrolments
- View enrolment status
- Cancel an enrolment where supported

The API prevents duplicate enrolments.

Organisers can view participants enrolled in their events.

### Results

Organisers can:

- Record participant results
- Record finishing times
- Record finishing positions
- View results for their events

Participants can view their own results, including:

- Event
- Category
- Finishing time
- Finishing position

---

## Azure Blob Storage

RaceDay uses Azure Blob Storage for image management.

Azure Blob Storage is accessed by RaceDay.API rather than directly by RaceDay.Mvc.

Images supported include:

- Participant/Organiser profile pictures
- Event banner images

Supported image formats:

- JPG
- JPEG
- PNG
- WEBP

Maximum image size:

- 5 MB

The application supports:

- Image upload
- Image replacement
- Image removal
- Removal of replaced images from Blob Storage
- Private Blob Storage access using generated read URLs

Azure Storage connection information is kept outside the source code using secure configuration/user secrets.

---

## Database

The application uses SQL Server and Entity Framework Core Code First.

Development database:

`RaceDayDB_Part2`

The database schema contains:

- Roles
- Users
- Events
- Categories
- Enrolments
- Results

EF Core migrations are used to create and maintain the development database.

The MVC application does not connect directly to the database. Database operations are performed through RaceDay.API.

---

## Running the Application Locally

### Prerequisites

Install:

- Visual Studio with ASP.NET and web development tools
- .NET 8 SDK
- SQL Server
- SQL Server Management Studio
- Docker Desktop
- Git

Azure Blob Storage configuration is required for image functionality.

### 1. Clone the Repository

Clone the RaceDay repository and open the solution in Visual Studio.

### 2. Configure the Database

Configure the RaceDay database connection string for RaceDay.API.

Apply the Entity Framework Core migrations if the database has not already been created.

### 3. Configure Azure Blob Storage

Configure the required Azure Storage settings for RaceDay.API using secure configuration or .NET user secrets.

Do not commit Azure Storage connection strings to source control.

### 4. Run RaceDay.API

From the solution directory:

```powershell
dotnet run --project RaceDay.API --launch-profile https
```

Development endpoints:

```text
https://localhost:7008
http://localhost:5226
```

Swagger:

```text
https://localhost:7008/swagger/index.html
```

### 5. Run RaceDay.Mvc

In a second terminal:

```powershell
dotnet run --project RaceDay.Mvc
```

MVC development address:

```text
http://localhost:5251
```

Open this address in a browser to use the RaceDay web application.

---

## Docker

RaceDay.Mvc supports Docker deployment.

The MVC Dockerfile is located at:

```text
RaceDay.Mvc/Dockerfile
```

The Docker image includes the required RaceDay.Contracts project dependency during restore and build.

### Start Docker Desktop

Ensure Docker Desktop is running before building or starting the container.

Verify Docker with:

```powershell
docker info
```

### Start RaceDay.API for Docker

For the local Docker demonstration, the MVC container communicates with RaceDay.API through the Windows host.

Start the API with HTTPS redirection disabled for the HTTP Docker-to-host development connection:

```powershell
$env:UseHttpsRedirection="false"
dotnet run --project RaceDay.API --launch-profile https
```

The API remains available through:

```text
http://localhost:5226
```

HTTPS redirection remains enabled by default when the configuration override is not supplied.

### Build the MVC Docker Image

Run this command from the solution root:

```powershell
docker build -f RaceDay.Mvc/Dockerfile -t raceday-mvc:latest .
```

### Run the MVC Docker Container

Run:

```powershell
docker run --name raceday-mvc-container -p 8080:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5226/ raceday-mvc:latest
```

Open:

```text
http://localhost:8080
```

The Dockerised MVC application communicates with the API using:

```text
http://host.docker.internal:5226/
```

`host.docker.internal` allows the Linux Docker container to communicate with RaceDay.API running on the Windows host.

### Stop the Container

Press:

```text
Ctrl+C
```

in the terminal running the container.

### Remove the Stopped Container

Before creating another container with the same name:

```powershell
docker rm raceday-mvc-container
```

---

## Docker Architecture

```text
Browser
   |
   v
http://localhost:8080
   |
   v
RaceDay.Mvc
Docker Container
   |
   | HTTP
   v
host.docker.internal:5226
   |
   v
RaceDay.API
   |
   +--------------------+
   |                    |
   v                    v
SQL Server       Azure Blob Storage
```

RaceDay.Mvc does not directly access SQL Server or Azure Blob Storage.

---

## Automated Testing

Automated tests are implemented using xUnit and EF Core InMemory.

The automated tests cover important API functionality including successful and failure scenarios.

Before committing changes, the solution can be tested with:

```powershell
dotnet test
```

Latest verified result:

```text
Failed: 0
```

The solution also builds successfully with:

```powershell
dotnet build
```

Latest verified build result:

```text
Build succeeded.
0 Error(s)
```

---

## Continuous Integration

GitHub Actions is used for continuous integration.

The workflow performs:

1. Repository checkout
2. .NET 8 setup
3. NuGet restore
4. Solution build
5. Automated tests

The CI workflow runs when configured repository events occur, including code pushes and pull requests.

The repository contains more than 20 meaningful Git commits documenting the development of the application.

---

## Security

Public registration creates Participant accounts.

Organiser privileges should be assigned through a trusted administrative process rather than allowing users to grant themselves the Organiser role.

Protected API endpoints use role-based authorisation and ownership checks.

Passwords are stored using ASP.NET Core password hashing rather than plain text.

Azure Storage credentials are not stored directly in application source code.

Private Blob Storage images are accessed through generated read URLs rather than making the storage container publicly accessible.

HTTPS redirection is enabled by default. The HTTP configuration override documented in the Docker section is intended for the local Docker-to-host development setup.

---

## User Interface

RaceDay uses a responsive Bootstrap-based interface.

The application provides separate role-aware navigation and functionality for:

### Participant

- Home / upcoming events
- Event details
- My Enrolments
- My Results
- My Profile

### Organiser

- Dashboard
- My Events
- Create Event
- Edit Event
- Category management
- Event participants
- Event results
- My Profile

Forms provide validation and feedback messages.

Save buttons are protected against accidental submissions by remaining disabled until genuine changes are detected.

Image upload buttons remain disabled until a file is selected.

---

## Part 2 Video Demonstration

The previous API/Part 2 demonstration is available at:

https://youtu.be/DIlPPcNDX2s

---

## Part 3 Video Demonstration

The final Part 3 demonstration will cover:

- ASP.NET Core MVC structure
- Participant functionality
- Organiser functionality
- RaceDay.API integration
- Azure Blob Storage
- Docker setup and execution
- Automated testing

YouTube link:

`To be added after the final Part 3 demonstration is recorded.`

---

## Author

Erwin Mashobane  
Student Number: ST10073464  
Programming 2B  
PROG6212