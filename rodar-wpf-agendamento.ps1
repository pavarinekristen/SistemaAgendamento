$ErrorActionPreference = "Stop"

Set-Location "C:\Users\krist\AgendamentoWpfApp"

$env:AGENDAMENTO_RETAGUARDA_URL = "http://localhost:5000"

dotnet run --project AgendamentoWpfApp.csproj
