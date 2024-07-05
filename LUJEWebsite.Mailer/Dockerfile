#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LUJEWebsite.Mailer/LUJEWebsite.Mailer.csproj", "LUJEWebsite.Mailer/"]
RUN dotnet restore "LUJEWebsite.Mailer/LUJEWebsite.Mailer.csproj"
COPY . .
WORKDIR "/src/LUJEWebsite.Mailer"
RUN dotnet build "LUJEWebsite.Mailer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LUJEWebsite.Mailer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LUJEWebsite.Mailer.dll", "sleep"]