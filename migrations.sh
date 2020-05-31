#!/bin/sh

dotnet run --project src/ChickenCheck.Migrations --connectionstring "Data Source=database.db;" $@