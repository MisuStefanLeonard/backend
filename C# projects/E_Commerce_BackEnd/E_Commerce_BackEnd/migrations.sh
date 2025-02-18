#!/bin/bash
#
#set -e  # Exit script on error
#echo "Starting migrations script"
## ✅ Ensure dotnet tools are available
#export PATH="$PATH:/root/.dotnet/tools"
#
## ✅ Apply pending migrations
#echo "🚀 Applying migrations..."
#dotnet ef database update --no-build
#
#echo "✅ Migrations applied successfully!"
#
#echo "Running Quartz initialization script..."
#
## Database credentials
#DB_HOST="dbtest.crume2y24a5h.eu-central-1.rds.amazonaws.com"
#DB_PORT="3306"
#DB_USER="admin"
#DB_PASSWORD="Stefan30122003!"
#DB_NAME="ComertDatabase"
#
#SQL_SCRIPT="./quartz_init.sql"
#
#mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" < "$SQL_SCRIPT"
#
#echo "Quartz initialization completed!"
#
#exit 0;



set -e  # Exit script on error

echo "🚀 Running database migrations..."

export PATH="$PATH:/root/.dotnet/tools"
dotnet ef database update --no-build --project /app/E_Commerce_BackEnd.dll

echo "✅ Migrations applied successfully!"

echo "🚀 Running Quartz SQL script..."
mysql -h "dbtest.crume2y24a5h.eu-central-1.rds.amazonaws.com" -P 3306 -u "admin" -p"Stefan30122003!" "ComertDatabase" < /app/quartz_init.sql
echo "✅ Quartz SQL script executed successfully!"

exit 0;
