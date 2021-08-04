FROM mcr.microsoft.com/dotnet/aspnet:5.0.8-alpine3.13-arm64v8

# Set TimeZone in docker container
RUN ln -snf /usr/share/zoneinfo/Europe/Stockholm /etc/localtime && echo Europe/Stockholm > /etc/timezone

# Copy 
WORKDIR /app
COPY ./output/server ./

EXPOSE 8085

ENTRYPOINT ["dotnet", "ChickenCheck.Backend.dll"]
