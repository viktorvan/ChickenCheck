FROM mcr.microsoft.com/dotnet/core/runtime:3.1.6-buster-slim-arm64v8

# Copy 
WORKDIR /app
COPY ./output/migrations ./migrations
COPY ./output/dbbackup ./dbbackup

ENTRYPOINT []
