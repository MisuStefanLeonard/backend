FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID

WORKDIR /app
#EXPOSE 8080
#EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:5043
EXPOSE 5043

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["E_Commerce_BackEnd/E_Commerce_BackEnd.csproj", "E_Commerce_BackEnd/"]
RUN dotnet restore "E_Commerce_BackEnd/E_Commerce_BackEnd.csproj"
COPY . .
WORKDIR "/src/E_Commerce_BackEnd"
RUN dotnet build "E_Commerce_BackEnd.csproj" -c $BUILD_CONFIGURATION -o /app/build

#FROM mcr.microsoft.com/dotnet/sdk:8.0
#RUN dotnet tool install --global dotnet-ef 
#RUN dotnet restore "E_Commerce_BackEnd/E_Commerce_BackEnd.csproj"
#COPY . .
#WORKDIR "/src/E_Commerce_BackEnd"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "E_Commerce_BackEnd.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "E_Commerce_BackEnd.dll" ]
#CMD ["sh", "-c", "dotnet ef database update && dotnet E_Commerce_BackEnd.dll"]
