FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.6-buster-slim

# Set TimeZone in docker container
RUN ln -snf /usr/share/zoneinfo/Europe/Stockholm /etc/localtime && echo Europe/Stockholm > /etc/timezone

# Copy 
WORKDIR /app
COPY ./output ./

EXPOSE 8085

ENTRYPOINT ["dotnet", "server/ChickenCheck.Backend.dll"]
