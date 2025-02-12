#!/bin/bash

set -e

# ✅ Ensure dotnet tools are available
export PATH="$PATH:/root/.dotnet/tools"

# ✅ Check if migrations need to be applied
PENDING_MIGRATIONS=$(dotnet ef migrations list | grep -v "No migrations applied")

if [[ -z "$PENDING_MIGRATIONS" ]]; then
  echo "✅ Database is already up to date. Skipping migrations."
  exit 0  # ✅ Exit successfully to mark container as completed
fi

# ✅ Apply pending migrations
until dotnet ef database update --no-build; do
  >&2 echo "Migration applying..."
  sleep 1
done

>&2 echo "✅ Migrations applied successfully!"
