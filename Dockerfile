FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Cloak/Cloak.csproj", "Cloak/"]
RUN dotnet restore "Cloak/Cloak.csproj"
COPY . .
WORKDIR "/src/Cloak"
RUN dotnet build "Cloak.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cloak.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cloak.dll"]
