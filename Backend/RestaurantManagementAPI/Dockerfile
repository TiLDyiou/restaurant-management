# Multi-stage production-ready Dockerfile for QLNH API (.NET 9.0)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 9000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["RestaurantManagementAPI/RestaurantManagementAPI.csproj", "RestaurantManagementAPI/"]
RUN dotnet restore "RestaurantManagementAPI/RestaurantManagementAPI.csproj"

# Copy all sources and build
COPY . .
WORKDIR "/src/RestaurantManagementAPI"
RUN dotnet build "RestaurantManagementAPI.csproj" -c Release -o /app/build

# Publish release assets
FROM build AS publish
RUN dotnet publish "RestaurantManagementAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Assemble the final minimal runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Ensure static directories and log folder exist
RUN mkdir -p wwwroot/uploads/chat wwwroot/uploads/dishes Logs

ENTRYPOINT ["dotnet", "RestaurantManagementAPI.dll"]
