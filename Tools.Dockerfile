FROM mcr.microsoft.com/dotnet/runtime:5.0.8-alpine3.13-arm64v8

# Copy 
WORKDIR /app
COPY ./output/migrations ./migrations
# COPY ./output/dbbackup ./dbbackup

ENTRYPOINT []
