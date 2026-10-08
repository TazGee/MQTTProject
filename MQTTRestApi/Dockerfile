# =========================
# 1. Base image
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app

EXPOSE 80
EXPOSE 443


# =========================
# 2. Build image
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["MQTTRestApi.csproj", "./"]

RUN dotnet restore "MQTTRestApi.csproj"

COPY . .

RUN dotnet build "MQTTRestApi.csproj" -c Debug -o /app/build


# =========================
# 3. Publish
# =========================
FROM build AS publish

RUN dotnet publish "MQTTRestApi.csproj" -c Debug -o /app/publish


# =========================
# 4. Final image
# =========================
FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MQTTRestApi.dll"]
