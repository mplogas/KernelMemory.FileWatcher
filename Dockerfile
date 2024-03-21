#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER 0 
RUN mkdir -p /data /config
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["KernelMemory.FileWatcher/KernelMemory.FileWatcher.csproj", "KernelMemory.FileWatcher/"]
RUN dotnet restore "./KernelMemory.FileWatcher/KernelMemory.FileWatcher.csproj"
COPY . .
WORKDIR "/src/KernelMemory.FileWatcher"
RUN dotnet publish "./KernelMemory.FileWatcher.csproj" -c $BUILD_CONFIGURATION -o /app/publish #/p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KernelMemory.FileWatcher.dll"]