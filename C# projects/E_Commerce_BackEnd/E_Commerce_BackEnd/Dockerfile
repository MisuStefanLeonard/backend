FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app
#EXPOSE 8080
#EXPOSE 8081
USER root
RUN apt-get update && apt-get install -y curl
RUN apt-get install -y mariadb-client
ENV ASPNETCORE_URLS=http://+:5043
EXPOSE 5043
USER $APP_UID
RUN dotnet tool install --global dotnet-ef


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["E_Commerce_BackEnd/E_Commerce_BackEnd.csproj", "E_Commerce_BackEnd/"]
RUN dotnet restore "E_Commerce_BackEnd/E_Commerce_BackEnd.csproj"
COPY . .
WORKDIR "/src/E_Commerce_BackEnd"

RUN dotnet build "E_Commerce_BackEnd.csproj" -c $BUILD_CONFIGURATION -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "E_Commerce_BackEnd.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

COPY ["E_Commerce_BackEnd/migrations.sh", "/app/migrations.sh"]
COPY ["E_Commerce_BackEnd/quartz_init.sql", "/app/quartz_init.sql"]

USER root
RUN chmod +x /app/migrations.sh
USER $APP_UID


# ✅ Ensure dotnet tools are available in PATH
ENV PATH="$PATH:/root/.dotnet/tools"

RUN /bin/bash /app/migrations.sh

ENTRYPOINT ["dotnet", "E_Commerce_BackEnd.dll" ]

