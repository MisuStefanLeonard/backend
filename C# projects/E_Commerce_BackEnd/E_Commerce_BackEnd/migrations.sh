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

# ✅ Apply pending migrations
echo "🚀 Applying migrations..."
dotnet ef database update --no-build

echo "✅ Migrations applied successfully!"
exit 0;