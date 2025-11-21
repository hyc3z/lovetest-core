# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy version file
COPY VERSION .

# Copy csproj and restore dependencies
COPY ActivationCodeApi.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .
COPY --from=build /src/VERSION .

# Create directory for database with proper permissions
RUN mkdir -p /app/data && chmod 777 /app/data

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Add version as build argument and label
ARG VERSION=unknown
LABEL version="${VERSION}"
LABEL maintainer="lovetest-api"
LABEL description="Activation Code API Service"

# Expose port
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "ActivationCodeApi.dll"]
