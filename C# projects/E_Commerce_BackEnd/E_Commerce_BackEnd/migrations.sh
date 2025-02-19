#!/bin/bash

set -e  # Exit script on error

echo "🚀 Running database migrations..."

dotnet tool list --global || { echo "❌ dotnet-ef is missing!"; exit 1; }

export PATH="$PATH:/root/.dotnet/tools"  
dotnet ef database update --no-build 


echo "✅ Migrations applied successfully!"

echo "🚀 Running Quartz SQL script..."
mysql -h "dbtest.crume2y24a5h.eu-central-1.rds.amazonaws.com" -P 3306 -u "admin" -p"Stefan30122003!" "ComertDatabase" < ./quartz_init.sql
echo "✅ Quartz SQL script executed successfully!"

exit 0;

