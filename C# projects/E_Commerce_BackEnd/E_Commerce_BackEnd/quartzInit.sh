#!/bin/bash

set -e  # Stop the script on the first error

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