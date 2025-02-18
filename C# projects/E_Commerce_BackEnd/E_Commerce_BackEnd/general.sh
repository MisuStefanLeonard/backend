#!/bin/bash

set -e  # Exit on any error

echo "Check is migrations.sh exists..."
ls -l /src/migrations.sh ||  { echo "❌ migrations.sh NOT FOUND!"; exit 1; }

echo "Running database migrations..."
chmod +x /src/migrations.sh
/bin/bash /src/migrations.sh

echo "Check is quartzInit.sh exists..."
ls -l /src/quartzInit.sh ||  { echo "❌ quartzInit.sh NOT FOUND!"; exit 1; }

chmod +x /src/quartzInit.sh
echo "Running Quartz initialization..."
/bin/bash /src/quartzInit.sh

echo "Starting the application..."
exit 0;