FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ENV PATH $PATH:/root/.dotnet/tools

WORKDIR /src
RUN apt-get update && apt-get install -y mariadb-client;

RUN dotnet tool install --global dotnet-ef

# ✅ Ensure dotnet tools are available in PATH
ENV PATH $PATH:/root/.dotnet/tools

# ✅ Copy only necessary source files (to avoid unnecessary rebuilds)
COPY E_Commerce_BackEnd /src/E_Commerce_BackEnd

WORKDIR "/src/E_Commerce_BackEnd"

# ✅ Ensure the script is executable
RUN chmod +x /src/migrations.sh

## ✅ Run migrations before starting the app
CMD ["/bin/bash", "/src/migrations.sh"]