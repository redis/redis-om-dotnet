FROM mcr.microsoft.com/dotnet/sdk:5.0


WORKDIR /app
ADD . /app

RUN ls /app
RUN dotnet restore /app/NRedisPlus.sln

ENTRYPOINT ["dotnet","test"]