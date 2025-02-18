
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# ✅ Copy only the essential files for restoring dependencies
COPY ["E_Commerce_BackEnd/E_Commerce_BackEnd.csproj", "E_Commerce_BackEnd/"]
RUN dotnet restore "E_Commerce_BackEnd/E_Commerce_BackEnd.csproj"

# ✅ Copy migration script
COPY ["E_Commerce_BackEnd/migrations.sh", "/src/migrations.sh"]
COPY ["E_Commerce_BackEnd/quartzInit.sh", "/src/quartzInit.sh"]


# ✅ Install dotnet-ef globally
RUN dotnet tool install --global dotnet-ef

# ✅ Ensure dotnet tools are available in PATH
ENV PATH="$PATH:/root/.dotnet/tools"

# ✅ Copy only necessary source files (to avoid unnecessary rebuilds)
COPY E_Commerce_BackEnd /src/E_Commerce_BackEnd

WORKDIR "/src/E_Commerce_BackEnd"

# ✅ Ensure the script is executable
RUN chmod +x /src/migrations.sh
RUN chmod +x /src/quartzInit.sh

COPY ["E_Commerce_BackEnd/general.sh", "/src/general.sh"]
RUN chmod +x /src/general.sh

ENTRYPOINT ["/bin/bash", "/src/general.sh"]

## ✅ Run migrations before starting the app
#CMD ["/bin/bash", "/src/migrations.sh"]
#CMD ["/bin/bash" , "/src/quartzInit.sh"]

