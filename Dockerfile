# ======== Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copie csproj et restaure les packages
COPY *.csproj ./
RUN dotnet restore

# Copie le reste du code et le publie
COPY . ./
RUN dotnet publish -c Release -o out


# ======== Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Expose le port (celui de l’API)
EXPOSE 8080
ENTRYPOINT ["dotnet", "EcommerceApi.dll"]