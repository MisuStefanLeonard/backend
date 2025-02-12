#!/bin/bash

set -e

# ✅ Ensure dotnet tools are available
export PATH="$PATH:/root/.dotnet/tools"

until dotnet ef database update --no-build; do
  >&2 echo "Migration applying..."
  sleep 1
done

>&2 echo "MySQL server is up - executing command"

dotnet ef database update
