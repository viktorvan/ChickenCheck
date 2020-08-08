FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.6-buster-slim-arm64v8

# Copy 
WORKDIR /app
COPY ./output/migrations ./

EXPOSE 8085

ENTRYPOINT ["dotnet", "ChickenCheck.Migrations.dll"]
