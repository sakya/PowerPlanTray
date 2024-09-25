rd /s /q .\dist\win-x64

dotnet clean PowerPlanTray.sln -c Release
dotnet publish PowerPlanTray\PowerPlanTray.csproj -c Release --runtime win-x64 -p:PublishReadyToRun=true --self-contained --output .\dist\win-x64
