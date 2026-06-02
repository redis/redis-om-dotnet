# Pull the .NET 7 runtime from its official image so the net7.0 test pass can run on the
# .NET 10 SDK base without fetching/executing a remote install script.
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS dotnet7

FROM mcr.microsoft.com/dotnet/sdk:10.0
COPY --from=dotnet7 /usr/share/dotnet/shared/Microsoft.NETCore.App /usr/share/dotnet/shared/Microsoft.NETCore.App

WORKDIR /app
ADD . /app

RUN ls /app
RUN dotnet restore /app/Redis.OM.sln

# Run the full solution on net7.0 (every test project targets it), then run the unit tests on
# net10.0 so the array-Contains regression is validated against the real C# 14 compiler.
ENTRYPOINT ["/bin/sh", "-c", "dotnet test --framework net7.0 && dotnet test test/Redis.OM.Unit.Tests/Redis.OM.Unit.Tests.csproj --framework net10.0"]
