FROM mcr.microsoft.com/dotnet/sdk:10.0

# The base image ships the .NET 10 SDK + runtime. Add the .NET 7 runtime so the existing
# net7.0 test pass can still execute alongside the net10.0 (C# 14) pass.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 7.0 --runtime dotnet --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh

WORKDIR /app
ADD . /app

RUN ls /app
RUN dotnet restore /app/Redis.OM.sln

# Run the full solution on net7.0 (every test project targets it), then run the unit tests on
# net10.0 so the array-Contains regression is validated against the real C# 14 compiler.
ENTRYPOINT ["/bin/sh", "-c", "dotnet test --framework net7.0 && dotnet test test/Redis.OM.Unit.Tests/Redis.OM.Unit.Tests.csproj --framework net10.0"]
