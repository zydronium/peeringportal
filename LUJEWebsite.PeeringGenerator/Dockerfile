#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LUJEWebsite.PeeringGenerator/LUJEWebsite.PeeringGenerator.csproj", "LUJEWebsite.PeeringGenerator/"]
RUN dotnet restore "LUJEWebsite.PeeringGenerator/LUJEWebsite.PeeringGenerator.csproj"
COPY . .
WORKDIR "/src/LUJEWebsite.PeeringGenerator"
RUN dotnet build "LUJEWebsite.PeeringGenerator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LUJEWebsite.PeeringGenerator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
RUN apt-get update && apt-get install -y bgpq4 ssh rsync
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LUJEWebsite.PeeringGenerator.dll"]