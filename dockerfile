FROM mcr.microsoft.com/dotnet/sdk:7.0

WORKDIR /app
ADD . /app

RUN ls /app
RUN dotnet restore /app/Redis.OM.sln

ENTRYPOINT ["dotnet", "test", "--framework", "net7.0" ]