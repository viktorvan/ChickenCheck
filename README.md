# Example of a web application built using the F# SAFE-stack with 

## To run locally

Make sure `FAKE` is installed and run `fake build --target run`

## Requirements

This project uses and SqlCommandProvider and requires a local database with a `sa` user with a specific password, see `migration.sh`. 

## Todos

1. The infrastructure setup and deployment in `build.fsx` is making use of a not yet published nuget package with a FAKE module for running Azure CLI commands. Maybe it can instead be referenced as a file from github using `paket`?

2. Add instructions on how to setup a local db in a docker container.