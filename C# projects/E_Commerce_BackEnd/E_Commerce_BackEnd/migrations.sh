#!/bin/bash

set -e

until /root/.dotnet/tools/dotnet ef database update --no-build; do
>&2 echo "Migration begin applying"
sleep 1
done 


>&2 echo "MySQL server is up - executing comand"

/root/.dotnet/tools/dotnet ef database update