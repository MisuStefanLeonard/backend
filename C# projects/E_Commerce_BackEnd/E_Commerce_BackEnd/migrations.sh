#!/bin/bash

set -e  # Exit script on error

echo "🚀 Running database migrations..."

dotnet tool list --global || { echo "❌ dotnet-ef is missing!"; exit 1; }

export PATH="$PATH:/root/.dotnet/tools"  
dotnet ef database update --no-build --configuration Release

# DE STERS MIGRARILE SI DE RULAT INIT DIN NOU ! REFRESH LA ATRIBUTE . S-AU PIERDUT 
echo "✅ Migrations applied successfully!"

echo "🚀 Running Quartz SQL script..."
#mysql -h "dbtest.crume2y24a5h.eu-central-1.rds.amazonaws.com" -P 3306 -u "admin" -p"Stefan30122003!" "ComertDatabase" < ./quartz_init.sql
#echo "✅ Quartz SQL script executed successfully!"

required_tables=(QRTZ_JOB_DETAILS QRTZ_TRIGGERS QRTZ_SIMPLE_TRIGGERS QRTZ_CRON_TRIGGERS QRTZ_SIMPROP_TRIGGERS QRTZ_BLOB_TRIGGERS QRTZ_CALENDARS QRTZ_PAUSED_TRIGGER_GRPS QRTZ_FIRED_TRIGGERS QRTZ_SCHEDULER_STATE QRTZ_LOCKS)

# Create a comma-separated list for the query
table_list=$(printf "'%s'," "${required_tables[@]}")
table_list=${table_list%,}  # Remove trailing comma

# Run the query to count the number of tables present
existing_count=$(mysql -h "dbtest.crume2y24a5h.eu-central-1.rds.amazonaws.com" -P 3306 -u "admin" -p"Stefan30122003!" "ComertDatabase" -s -N -e "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='ComertDatabase' AND TABLE_NAME IN ($table_list);")

expected_count=${#required_tables[@]}

if [ "$existing_count" -eq "$expected_count" ]; then
    echo "All required tables exist. Skipping table/index creation."
else
    echo "Some required tables are missing. Running creation script..."
    mysql -h "dbtest.crume2y24a5h.eu-central-1.rds.amazonaws.com" -P 3306 -u "admin" -p"Stefan30122003!" "ComertDatabase" < ./quartz_init.sql
fi

exit 0;

