#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#ENV PATH $PATH:/root/.dotnet/tools
#
#WORKDIR /src
#RUN apt-get update && apt-get install -y mariadb-client;
#
#RUN dotnet tool install --global dotnet-ef
#
## ✅ Ensure dotnet tools are available in PATH
#ENV PATH $PATH:/root/.dotnet/tools
#
## ✅ Copy only necessary source files (to avoid unnecessary rebuilds)
#COPY E_Commerce_BackEnd /src/E_Commerce_BackEnd
#
#WORKDIR "/src/E_Commerce_BackEnd"
#
## ✅ Ensure the script is executable
#RUN chmod +x ./migrations.sh
#
#RUN dotnet build "E_Commerce_BackEnd.csproj" -c Release
#
### ✅ Run migrations before starting the app
#CMD ["/bin/bash", "./migrations.sh"]


# Use the .NET SDK image for building and running migrations
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Add dotnet tools to PATH
ENV PATH="/root/.dotnet/tools:$PATH"

# Set working directory
WORKDIR /src

# Install required dependencies (e.g. mariadb-client)
RUN apt-get update && apt-get install -y mariadb-client

# Install the dotnet-ef tool globally
RUN dotnet tool install --global dotnet-ef

# Copy only the project file first to leverage Docker cache during restore
COPY E_Commerce_BackEnd/E_Commerce_BackEnd.csproj E_Commerce_BackEnd/

# Switch to the project directory and restore dependencies
WORKDIR /src/E_Commerce_BackEnd
RUN dotnet restore

# Now copy the rest of the source code
COPY E_Commerce_BackEnd/ ./

# Ensure the migration script is executable
RUN chmod +x ./migrations.sh

# Build the project in Release configuration (this generates all needed artifacts)
RUN dotnet build "E_Commerce_BackEnd.csproj" -c Release

# At runtime, execute the migration script.
CMD ["/bin/bash", "./migrations.sh"]
