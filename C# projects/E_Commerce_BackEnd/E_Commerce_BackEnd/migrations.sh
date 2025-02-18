#!/bin/bash

set -e  # Exit script on error
echo "Starting migrations script"
# ✅ Ensure dotnet tools are available
export PATH="$PATH:/root/.dotnet/tools"

# ✅ Check if migrations need to be applied
PENDING_MIGRATIONS=$(dotnet ef migrations list | grep -v "No migrations applied" || true)

if [[ -z "$PENDING_MIGRATIONS" ]]; then
  echo "✅ Database is already up to date. Skipping migrations."
  exit 0  # ✅ Exit successfully to mark container as completed
fi

echo "Running Quartz initialization script..."

# Database credentials
DB_HOST="dbtest.crume2y24a5h.eu-central-1.rds.amazonaws.com"
DB_PORT="3306"
DB_USER="admin"
DB_PASSWORD="Stefan30122003!"
DB_NAME="ComertDatabase"

SQL_SCRIPT="./quartz_init.sql"

mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" < "$SQL_SCRIPT"

echo "Quartz initialization completed!"

# ✅ Apply pending migrations
echo "🚀 Applying migrations..."
dotnet ef database update --no-build

echo "✅ Migrations applied successfully!"
exit 0;