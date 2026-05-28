
# ⚡ PanelApp

![.NET](https://img.shields.io/badge/.NET-8-blue)
![ASP.NET MVC](https://img.shields.io/badge/ASP.NET-MVC-green)
![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)
![Status](https://img.shields.io/badge/Status-Development-orange)

> Internal ERP-style platform for managing electrical distribution panel production, quotations, material pricing, supplier relationships and project costing.

----



# 📑 Table of Contents

* [General Information](#general-information)
* [Current Capabilities](#current-capabilities)
* [Technologies](#technologies)
* [Architecture](#architecture)
* [Folder Structure](#folder-structure)
* [UI Preview](#ui-preview)
* [Development Setup](#development-setup)
* [Setup DB](#setup-db)
* [Features](#features)
* [Code Highlights](#code-highlights)
* [Excel Import](#excel-import)
* [Authentication](#authentication)
* [AI Assistant](#ai-assistant)
* [Gemini Integration](#gemini-integration)
* [Future Improvements](#future-improvements)
* [Acknowledgements](#acknowledgements)


----


# 📌General Information

PanelApp is an ASP.NET Core MVC ERP-style platform designed for electrical distribution panel manufacturing workflows.

The platform focuses on:
- panel costing
- quotation management
- material catalog management
- supplier & customer organization
- AI-assisted quotation workflows
- Excel-based imports
- production workflow support
- operational tracking

----

# 🚀Current Capabilities

✅ Panel management  
✅ Offer / quotation management  
✅ AI-powered quotation assistant  
✅ AI quotation preview workflow  
✅ Material catalog management  
✅ Supplier & customer management  
✅ Excel imports  
✅ Activity logging  
✅ Role-based authentication  
✅ Dark / Light mode  
✅ Snapshot pricing logic  
✅ Material auto-matching  
✅ Cabinet auto-matching  
✅ Extra item support  
✅ Labor & profit calculations  
✅ Responsive Bootstrap UI  

## 🤖 Ai Assistant
✅ AI material search  
✅ AI offer operations  
✅ Offer summary assistant  
✅ PDF offer export  
✅ Excel offer export  
✅ Catalog pricing visibility  
✅ Responsive offer workspace  

The assistant also supports:

- material search inside the ERP
- offer summary generation
- inline offer modifications
- contextual operations inside opened offers
- guided offer preview workflow

----

# 🧱Technologies

- ASP.NET Core MVC (.NET 8)
- Entity Framework Core
- SQL Server
- Bootstrap 5
- Bootstrap Icons
- ClosedXML
- Session-based Authentication
- LINQ / EF Core Query Optimization
- Google Gemini API
- JSON-based AI parsing

----

# 🏗️Architecture

```text
[ Browser UI ]
       |
       v
[ MVC Controllers ]
       |
       +-------------------+
       |                   |
       v                   v
[ AI Services ]     [ Business Logic ]
       |                   |
       +---------+---------+
                 |
                 v
      [ Entity Framework Core ]
                 |
                 v
           [ SQL Server ]
```

```mermaid
flowchart TD

    subgraph UI["Frontend UI Layer"]
        A[User opens AI Chat Popup]
        B[User writes natural language quotation request]
        C[ai-chat.js sends AJAX POST request]
    end

    subgraph MVC["ASP.NET MVC Layer"]
        D[AIController.Chat]
        E[OfferPreview.cshtml]
        F[CreateOfferFromPreview]
    end

    subgraph AISERVICES["AI Service Layer"]
        G[IOfferAiParser Interface]
        H[OfferAiParser Service]
        I[Prompt Engineering]
        J[JSON Sanitization]
        K[Structured Draft Parsing]
    end

    subgraph GEMINI["Google Gemini API"]
        L[Gemini Flash Model]
        M[LLM Natural Language Understanding]
        N[Structured JSON Response]
    end

    subgraph BUSINESS["Business Logic Layer"]
        O[Resolve Customer]
        P[Resolve Materials]
        Q[Resolve Cabinets]
        R[Validate Extra Items]
        S[Calculate Totals]
        T[Preview Validation]
    end

    subgraph DATA["Persistence Layer"]
        U[Entity Framework Core]
        V[(SQL Server)]
    end

    subgraph AUDIT["Operational Tracking"]
        W[Activity Logger]
        X[AI Preview Logs]
        Y[AI Offer Creation Logs]
    end

    A --> B
    B --> C
    C --> D

    D --> G
    G --> H

    H --> I
    I --> J
    J --> L

    L --> M
    M --> N

    N --> K
    K --> D

    D --> O
    D --> P
    D --> Q
    D --> R

    O --> S
    P --> S
    Q --> S
    R --> S

    S --> T
    T --> E

    E -->|User Confirms| F

    F --> U
    U --> V

    D --> W
    F --> W

    W --> X
    W --> Y

    classDef ui fill:#2563eb,color:#ffffff,stroke:#1e3a8a
    classDef mvc fill:#7c3aed,color:#ffffff,stroke:#581c87
    classDef ai fill:#0f766e,color:#ffffff,stroke:#134e4a
    classDef gem fill:#ea580c,color:#ffffff,stroke:#9a3412
    classDef logic fill:#16a34a,color:#ffffff,stroke:#166534
    classDef data fill:#dc2626,color:#ffffff,stroke:#7f1d1d
    classDef audit fill:#475569,color:#ffffff,stroke:#0f172a

    class A,B,C ui
    class D,E,F mvc
    class G,H,I,J,K ai
    class L,M,N gem
    class O,P,Q,R,S,T logic
    class U,V data
    class W,X,Y audit
```

----


# 📁Folder Structure

Detailed:
```text
ZL_panelapp/
│
├── Controllers/
│   ├── PanelsController.cs
│   ├── AiController.cs
│   ├── MaterialsController.cs
│   ├── HomeController.cs
│   ├── SuppliersController.cs
│   ├── ActivityLogsController.cs
│   ├── CustomersController.cs
│   ├── MaterialsController.cs
│   └── AccountController.cs
│
├── Models/
│	├── AI/OfferAiOperation.cs
│   ├── Panel.cs
│   ├── PanelMaterial.cs
│   ├── PanelCabinet.cs
│   ├── PanelExtraItem.cs
│   ├── Offer.cs
│   ├── OfferMaterial.cs
│   ├── OfferCabinet.cs
│   ├── OfferExtraItem.cs
│   ├── Material.cs
│   ├── Supplier.cs
│   ├── ActivityLog.cs
│   ├── Customer.cs
│   ├── Cabinet.cs
│   ├── SupplierContactPerson.cs
│   └── User.cs
│
├── Views/
│   ├── AI/
│   ├── Panels/
│   ├── Offers/
│   ├── Materials/
│   ├── Suppliers/
│   ├── Home/
│   ├── ActivityLogs/
│   ├── Customers/
│   ├── Account/
│   └── Shared/
│       ├── _Layout.cshtml
│       └── _AuthLayout.cshtml
│
├── Data/
│   └── ApplicationDbContext.cs
├── Services/
│   ├── AI/Chat
│   │	 	└──AiChatIntentService.cs
│   │	 	└──AiChatRouterService.cs
│   ├── AI/Helpers/
│   ├── ActivityLogService.cs
│   ├── CabinetImportService.cs
│	'
│	'
│	'
│   └── PanelService.cs
│
├── ViewModels/
│   ├── AddMaterialToPanelViewModel.cs
│   ├── CopyPanelViewModel.cs
│   ├── CustomerIndexViewModel.cs
│   ├── EditPanelMaterialAdminViewModel.cs/
│	'
│	'
│	'
│   └── SupplierIndexViewModel.cs
│
└── wwwroot/
    └── css / js / images
```

----

# 🖼️UI Preview

👉 Replace with real screenshots for production

## Login

![Login](docs/screenshots/login.png)

## Dashboard

![Dashboard](docs/screenshots/dashboard.png)

## Materials

![Materials](docs/screenshots/materials.png)

## Panels

![Materials](docs/screenshots/panels.png)


----

# 🛠️Development Setup Guide

## Prerequisites

* Visual Studio 2022+
* .NET SDK 8
* SQL Server / SQL Express
* Git

### Verify

```bash
dotnet --version
sqlcmd -?
```

----

## Clone



```bash
git clone <repo>
cd panelapp
```

----

## Run

```bash
dotnet run
```

----
# 🗄️SetupDB

For the purpose of our implementation we developed our database in [SQL Server 2022](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
## Install SQL Express

### On windows

Steps:

1. Download:
SQL Server (Express or Developer)
SSMS (management tool)
2. Run the installer → select:
    - Basic (quick)
    - Custom (recommended)
3. In the setup:

    Instance:
   - MSSQLSERVER (default)
   - Authentication:
  -- Windows + SQL Server (Mixed Mode)

    Set a password for sa

4. Install:
5. Open SSMS and connect:
```
 Server: localhost
 Auth: Windows Authentication
```
----

### Powershell
```
// Installer
Invoke-WebRequest -Uri https://go.microsoft.com/fwlink/?linkid=866662 -OutFile SQLServer.exe

// Silent install
Start-Process -Wait -FilePath .\SQLServer.exe -ArgumentList "/Q /ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=MSSQLSERVER /SECURITYMODE=SQL /SAPWD=YourStrong!Pass123 /IACCEPTSQLSERVERLICENSETERMS"

// Open port
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow

// Check service
Get-Service -Name MSSQLSERVER

```
----

### On Linux

Steps:

```
// Add Microsoft repo
$ curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
 sudo add-apt-repository "$(curl https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list)"

// Update
$ sudo apt update
$ sudo apt install -y mssql-server

// Setup
$ sudo /opt/mssql/bin/mssql-conf setup

// Start service
$ sudo systemctl status mssql-server

// Connect
$ sqlcmd -S localhost -U sa -P 'YourPassword'
```

For version, select 
- Edition (Developer = free)
- Password for _sa_

----

## Create DB
```sql
USE paneldb;
GO

CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    RoleName NVARCHAR(50) NOT NULL,
    Active BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
```

----



## Connection String
For named sql server: `SQLEXPRESS` and database: `paneldb`

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YourServer\\SQLEXPRESS;Database=paneldb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

----

## Seed
```sql
USE paneldb;
GO

-- USERS
INSERT INTO Users (Username, PasswordHash, FullName, RoleName, Active)
VALUES
('admin', 'admin', N'User Administrator', 'Admin', 1),
('user', 'user', N'User Demo', 'User', 1);
GO
```

----


# 📦Features

```mermaid
flowchart TD
    A[Application Features] --> B[Panel creation & cost calculation]
    A --> C[Material catalog]
    A --> D[Supplier management]
    A --> E[Excel import]
    A --> F[Export]
	A --> G[Offers]
	A --> H[AI Bot]

    E --> E1[Insert]
    E --> E2[Update]

    F --> F1[Excel]
    F --> F2[CSV]
	
	G --> G1[Convert To Panel]
	G --> GG1[Material Search]
	
	G --> G3[AI Offer Operations]

	G3 --> GG1[Offer Preview]
	G3 --> GG2[Material Search]
	G3 --> GG3[Offer Summary]


G --> G3[AI Offer Operations]
G --> G4[Offer PDF Export]
G --> G5[Offer Excel Export]

H --> H1[Material Search]
H --> H2[Offer Summary]

    style A fill:#1f,color:#ffffff,stroke:#111827,stroke-width:2px
    style B fill:#8b5cf6,color:#ffffff,stroke:#7c3aed
    style C fill:#3b82f6,color:#ffffff,stroke:#2563eb
    style D fill:#22c55e,color:#ffffff,stroke:#16a34a
    style E fill:#f97316,color:#ffffff,stroke:#ea580c
    style F fill:#ef4444,color:#ffffff,stroke:#dc2626
	style G fill:#a12316,color:#ffffff,stroke:#eb580d
    style H fill:#bf4344,color:#ffffff,stroke:#dc1626
    style E1 fill:#fed7aa,color:#111827,stroke:#f97316
    style E2 fill:#fed7aa,color:#111827,stroke:#f97316
    style F1 fill:#fecaca,color:#111827,stroke:#ef4444
    style F2 fill:#fecaca,color:#111827,stroke:#ef4444
```

----

# 💻Code Highlights

## Import Logic

```csharp
if (existing == null)
{
    _context.Materials.Add(new Material { ... });
}
else
{
    existing.CurrentPrice = price;
}
```

----

## Transaction Safety

```csharp
await transaction.CommitAsync();
```

## HTTPS

```csharp
await transaction.CommitAsync();
```


----


# 📊Excel Import

Mandatory supplier selection from UI.

Format:

```text
MaterialCode | Description | Price | Unit
```

Rules:

* One supplier per file
* No duplicates
* Update existing

----

# 🔐Authentication 

**Admin**

* Full access
* Excel imports
* Supplier management
* Customer management
* Activity log access
* Panel editing

**User**

* Panel management
* Materials browsing
* Limited activity visibility

----


# 🤖AI Assistant

PanelApp includes an integrated AI assistant focused on accelerating quotation workflows.

The assistant can:
- generate quotation drafts from natural language
- identify customers from prompts
- resolve materials from the catalog
- resolve cabinet references
- create custom extra items
- estimate labor & profit
- validate unresolved catalog lines
- generate preview workflows before persistence

Example prompt:

```text
Create an offer for customer X with:
2x ODE-3-120023-1F12
1x cabinet CAB-001
20 meters testing cable at 1.50€/m
Labor 100€
Profit 50€
```

Workflow:
1. User submits prompt
2. AI generates structured draft
3. System validates references
4. Preview is generated
5. User confirms creation
6. Offer is persisted to SQL Server

----

# 🤖Gemini Integration
```mermaid
flowchart TD

    subgraph UI["Frontend"]
        A[User opens AI Chat]
        B[ai-chat.js]
        C[Quick Actions]
    end

    subgraph MVC["ASP.NET MVC"]
        D[AIController.Chat]
        E[Offer Preview]
        F[Offer Details]
    end

    subgraph ROUTER["AI Chat Layer"]
        G[AiChatRouterService]
        H[AiChatIntentService]
        I[Intent Detection]
    end

    subgraph INTENTS["Supported AI Intents"]
        J[Offer Create]
        K[Offer Operations]
        L[Material Search]
        M[Offer Summary]
        N[Help / Scoped Responses]
    end

    subgraph GEMINI["Gemini AI"]
        O[OfferAiParser]
        P[Prompt Engineering]
        Q[Gemini Flash API]
        R[Structured JSON Draft]
    end

    subgraph BUSINESS["Business Logic"]
        S[Resolve Customer]
        T[Resolve Materials]
        U[Resolve Cabinets]
        V[Resolve Extra Items]
        W[Offer Calculations]
        X[Offer Preview Validation]
    end

    subgraph EXECUTION["Offer Operations"]
        Y[OfferAiOperationParser]
        Z[OfferAiOperationExecutor]
        AA[Update Offer Materials]
        AB[Update Cabinets]
        AC[Update Extra Items]
    end

    subgraph DATA["Persistence"]
        AD[Entity Framework Core]
        AE[(SQL Server)]
    end

    subgraph AUDIT["Logging"]
        AF[Activity Logger]
        AG[AI Operation Logs]
        AH[AI Offer Preview Logs]
    end

    A --> B
    B --> D
    C --> B

    D --> G
    G --> H
    H --> I

    I --> J
    I --> K
    I --> L
    I --> M
    I --> N

    J --> O
    O --> P
    P --> Q
    Q --> R

    R --> S
    R --> T
    R --> U
    R --> V

    S --> W
    T --> W
    U --> W
    V --> W

    W --> X
    X --> E

    K --> Y
    Y --> Z

    Z --> AA
    Z --> AB
    Z --> AC

    AA --> AD
    AB --> AD
    AC --> AD

    E --> AD
    AD --> AE

    D --> AF
    Z --> AG
    E --> AH

    classDef ui fill:#2563eb,color:#ffffff,stroke:#1e3a8a
    classDef mvc fill:#7c3aed,color:#ffffff,stroke:#581c87
    classDef ai fill:#0f766e,color:#ffffff,stroke:#134e4a
    classDef gem fill:#ea580c,color:#ffffff,stroke:#9a3412
    classDef logic fill:#16a34a,color:#ffffff,stroke:#166534
    classDef data fill:#dc2626,color:#ffffff,stroke:#7f1d1d
    classDef audit fill:#475569,color:#ffffff,stroke:#0f172a

    class A,B,C ui
    class D,E,F mvc
    class G,H,I,J,K,L,M,N ai
    class O,P,Q,R gem
    class S,T,U,V,W,X,Y,Z,AA,AB,AC logic
    class AD,AE data
    class AF,AG,AH audit
```

----

# 🖥️VM Setup
Assuming that the application is developed within the VM
* Hosting Bundle
* IIS
* mssql-server

Minimum
* Windows 8 VM 80+ GB
* 4GB RAM+
* SQL Express 2018


Recommended
* Windows 11 VM 150GB+
* 8GB RAM+
* SQL Express 2022


----

# 🔮Future Improvements

- AI panel generation
- Semantic search
- Dashboard AI insights
- Cost optimization suggestions

![Future Diagram](graphviz.png)

----

# 🙏Acknowledgements

Developed for recording electrical distribution panel equipment for the company **Company**.

v0.3.4 – AI Offer Operations & Responsive Workspace Refactor


```
TO BE CONTINUED
```
----
