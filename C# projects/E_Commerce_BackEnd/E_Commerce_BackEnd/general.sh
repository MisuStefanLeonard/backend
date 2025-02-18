#!/bin/bash

set -e  # Exit on any error

echo "Running database migrations..."
/bin/bash /src/migrations.sh

echo "Running Quartz initialization..."
/bin/bash /src/quartzInit.sh

echo "Starting the application..."
