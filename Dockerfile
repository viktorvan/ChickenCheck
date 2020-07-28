FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.6-buster-slim

# Copy 
WORKDIR /app
COPY ./output ./

EXPOSE 8085

ENTRYPOINT ["dotnet", "server/ChickenCheck.Backend.dll"]
