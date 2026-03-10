# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy restore inputs first to maximize layer cache hits.
COPY E-Commerce-Microservices-Demo.sln ./
COPY src/ProductService/ProductService.csproj src/ProductService/
RUN dotnet restore src/ProductService/ProductService.csproj

# Copy the rest and publish.
COPY . .
RUN dotnet publish src/ProductService/ProductService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Container listens on 8080; map host port with docker run/compose.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]
