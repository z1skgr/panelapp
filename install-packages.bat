dotnet add package Microsoft.EntityFrameworkCore --version 8.*
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.*
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.*

dotnet add package Microsoft.Extensions.Identity.Core --version 8.*
dotnet add package Microsoft.Extensions.Logging.EventLog --version 8.*

dotnet add package ClosedXML
dotnet add package Newtonsoft.Json

dotnet add package QuestPDF

dotnet restore
dotnet build

pause