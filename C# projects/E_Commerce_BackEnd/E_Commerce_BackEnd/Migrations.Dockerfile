FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
RUN apt-get update && apt-get install -y mariadb-client
# ✅ Copy only the essential files for restoring dependencies
COPY ["E_Commerce_BackEnd/E_Commerce_BackEnd.csproj", "E_Commerce_BackEnd/"]
RUN dotnet restore "E_Commerce_BackEnd/E_Commerce_BackEnd.csproj"

# ✅ Copy migration script
COPY ["E_Commerce_BackEnd/migrations.sh", "/src/migrations.sh"]

# ✅ Install dotnet-ef globally
RUN dotnet tool install --global dotnet-ef

# ✅ Ensure dotnet tools are available in PATH
ENV PATH="$PATH:/root/.dotnet/tools"


# ✅ Copy only necessary source files (to avoid unnecessary rebuilds)
COPY E_Commerce_BackEnd /src/E_Commerce_BackEnd

WORKDIR "/src/E_Commerce_BackEnd"

# ✅ Ensure the script is executable
RUN chmod +x /src/migrations.sh

## ✅ Run migrations before starting the app
CMD ["/bin/bash", "/src/migrations.sh"]




