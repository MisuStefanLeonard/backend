FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["E_Commerce_BackEnd/E_Commerce_BackEnd.csproj", "E_Commerce_BackEnd/"]
COPY migrations.sh migrations.sh

RUN dotnet tool install --global dotnet-ef

RUN dotnet restore "E_Commerce_BackEnd/E_Commerce_BackEnd.csproj"
COPY . .
WORKDIR "/src/E_Commerce_BackEnd"

RUN /root/
