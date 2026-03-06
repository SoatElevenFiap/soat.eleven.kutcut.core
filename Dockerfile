FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["soat.eleven.kutcut.core/soat.eleven.kutcut.core/soat.eleven.kutcut.core.api.csproj", "soat.eleven.kutcut.core/soat.eleven.kutcut.core/"]
COPY ["soat.eleven.kutcut.core/soat.eleven.kutcut.application/soat.eleven.kutcut.application.csproj", "soat.eleven.kutcut.core/soat.eleven.kutcut.application/"]
COPY ["soat.eleven.kutcut.core/soat.eleven.kutcut.domain/soat.eleven.kutcut.domain.csproj", "soat.eleven.kutcut.core/soat.eleven.kutcut.domain/"]
COPY ["soat.eleven.kutcut.core/soat.eleven.kutcut.infra/soat.eleven.kutcut.infra.csproj", "soat.eleven.kutcut.core/soat.eleven.kutcut.infra/"]

RUN dotnet restore "soat.eleven.kutcut.core/soat.eleven.kutcut.core/soat.eleven.kutcut.core.api.csproj"

COPY . .
WORKDIR "/src/soat.eleven.kutcut.core/soat.eleven.kutcut.core"
RUN dotnet build "soat.eleven.kutcut.core.api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "soat.eleven.kutcut.core.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage for migrations
FROM build AS migrator
RUN dotnet tool install --global dotnet-ef --version 8.*
ENV PATH="/root/.dotnet/tools:${PATH}"
WORKDIR /src

# Base image for production
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "soat.eleven.kutcut.core.api.dll"]
