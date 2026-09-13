# Step 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project definition and restore dependencies as distinct layer for caching
COPY ["server.csproj", "./"]
RUN dotnet restore "server.csproj"

# Copy source code and publish the Release artifact
COPY . .
RUN dotnet publish "server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Step 2: Production runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as unprivileged user for security
USER $APP_UID

COPY --from=build /app/publish .

# Default ASP.NET Core port
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "server.dll"]
