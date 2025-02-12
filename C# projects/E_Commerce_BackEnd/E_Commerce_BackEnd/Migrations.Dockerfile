FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["E_Commerce_BackEnd/E_Commerce_BackEnd.csproj", "E_Commerce_BackEnd/"]
COPY ["E_Commerce_BackEnd/migrations.sh", "/src/migrations.sh"]

RUN dotnet tool install --global dotnet-ef
RUN dotnet restore "E_Commerce_BackEnd/E_Commerce_BackEnd.csproj"
COPY . .
WORKDIR "/src/E_Commerce_BackEnd"

# ✅ Make sure the script is executable
RUN chmod +x ./migrations.sh

# ✅ Run migrations and then start the app
CMD ["/bin/bash", "./migrations.sh"]
