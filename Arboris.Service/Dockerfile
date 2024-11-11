# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-windowsservercore-ltsc2022 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-nanoserver-ltsc2022 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Arboris.Service/Arboris.Service.csproj", "Arboris.Service/"]
COPY ["Arboris.EntityFramework/Arboris.EntityFramework.csproj", "Arboris.EntityFramework/"]
COPY ["Arboris/Arboris.csproj", "Arboris/"]
RUN dotnet restore "./Arboris.Service/Arboris.Service.csproj"
COPY . .
WORKDIR "/src/Arboris.Service"
RUN \
  dotnet add  package libclang.runtime.win-x64 --version 18.1.3.2 && \
  dotnet add  package libClangSharp.runtime.win-x64 --version 18.1.3.1
RUN dotnet build "./Arboris.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Arboris.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
RUN curl -SL --output vs_buildtools.exe https://aka.ms/vs/17/release/vs_buildtools.exe
RUN vs_buildtools.exe --installPath C:\BuildTools --add Microsoft.VisualStudio.Component.VC.Tools.x86.x64 --add Microsoft.VisualStudio.Component.VC.ATL --add Microsoft.VisualStudio.Component.Windows11SDK.22621 --quiet --wait --norestart --nocache
RUN del vs_buildtools.exe
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Arboris.Service.dll"]