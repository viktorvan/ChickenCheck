FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.4-alpine3.11-arm64v8

# Copy 
WORKDIR /app
COPY ./output ./

EXPOSE 8085

ENTRYPOINT ["dotnet", "server/ChickenCheck.Backend.dll"]