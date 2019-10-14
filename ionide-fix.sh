#!/bin/sh
DIRS="src/ChickenCheck.Domain src/ChickenCheck.Infrastructure src/ChickenCheck.Backend src/ChickenCheck.Backend.Local src/ChickenCheck.Client src/ChickenCheck.PasswordHasher.Console src/ChickenCheck.Migrations"
for dir in $DIRS; do
	rm -rf $dir/bin $dir/obj
done
for dir in $DIRS; do
	dotnet build $dir
done