FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["E_Commerce_BackEnd/E_Commerce_BackEnd.csproj", "E_Commerce_BackEnd/"]
COPY ["E_Commerce_BackEnd/migrations.sh", "/src/migrations.sh"]

# ✅ Install dotnet-ef globally
RUN dotnet tool install --global dotnet-ef

# ✅ Ensure dotnet tools are available in PATH
ENV PATH="$PATH:/root/.dotnet/tools"

RUN dotnet restore "E_Commerce_BackEnd/E_Commerce_BackEnd.csproj"
COPY . .
WORKDIR "/src/E_Commerce_BackEnd"

# ✅ Ensure script is executable
RUN chmod +x /src/migrations.sh

# ✅ Run migrations before starting the app
CMD ["/bin/bash", "/src/migrations.sh"]
