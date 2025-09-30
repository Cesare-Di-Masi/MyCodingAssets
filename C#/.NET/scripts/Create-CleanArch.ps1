param(
    [string]$SolutionName = "CleanArchSolution"
)

# Crea cartella soluzione
New-Item -ItemType Directory -Path $SolutionName
Set-Location $SolutionName

# Crea la solution
dotnet new sln -n $SolutionName

# --- Progetti ---
# Core (Domain)
dotnet new classlib -n $SolutionName.Core -o src/Core
# Application
dotnet new classlib -n $SolutionName.Application -o src/Application
# Infrastructure
dotnet new classlib -n $SolutionName.Infrastructure -o src/Infrastructure
# WebApi (Presentation)
dotnet new webapi -n $SolutionName.WebApi -o src/WebApi
# Tests
dotnet new xunit -n $SolutionName.Tests -o tests

# --- Riferimenti ---
# Application dipende da Core
dotnet add src/Application/$SolutionName.Application.csproj reference src/Core/$SolutionName.Core.csproj

# Infrastructure dipende da Core e Application
dotnet add src/Infrastructure/$SolutionName.Infrastructure.csproj reference src/Core/$SolutionName.Core.csproj
dotnet add src/Infrastructure/$SolutionName.Infrastructure.csproj reference src/Application/$SolutionName.Application.csproj

# WebApi dipende da Application e Infrastructure
dotnet add src/WebApi/$SolutionName.WebApi.csproj reference src/Application/$SolutionName.Application.csproj
dotnet add src/WebApi/$SolutionName.WebApi.csproj reference src/Infrastructure/$SolutionName.Infrastructure.csproj

# Tests dipendono da Application e Core
dotnet add tests/$SolutionName.Tests.csproj reference src/Application/$SolutionName.Application.csproj
dotnet add tests/$SolutionName.Tests.csproj reference src/Core/$SolutionName.Core.csproj

# --- Aggiungi progetti alla solution ---
dotnet sln add src/Core/$SolutionName.Core.csproj
dotnet sln add src/Application/$SolutionName.Application.csproj
dotnet sln add src/Infrastructure/$SolutionName.Infrastructure.csproj
dotnet sln add src/WebApi/$SolutionName.WebApi.csproj
dotnet sln add tests/$SolutionName.Tests.csproj

Write-Host "✅ Soluzione $SolutionName creata con architettura clean."
