FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["itey.csproj", "./"]
RUN dotnet restore "itey.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "itey.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "itey.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final

RUN apt-get update && apt-get install -y \
    screen \
    curl \
    lsof \
    vim \
    strace

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "itey.dll"]
