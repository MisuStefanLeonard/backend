#!/bin/bash
#
#set -e
#
## ✅ Ensure dotnet tools are available
#export PATH="$PATH:/root/.dotnet/tools"
#
## ✅ Check if migrations need to be applied
#PENDING_MIGRATIONS=$(dotnet ef migrations list | grep -v "No migrations applied")
#
#if [[ -z "$PENDING_MIGRATIONS" ]]; then
#  echo "✅ Database is already up to date. Skipping migrations."
#  exit 0  # ✅ Exit successfully to mark container as completed
#fi
#
## ✅ Apply pending migrations
#until dotnet ef database update --no-build; do
#  >&2 echo "Migration applying..."
#  sleep 1
#done
#
#>&2 echo "✅ Migrations applied successfully!"


set -e

# ✅ Ensure dotnet tools are available
export PATH="$PATH:/root/.dotnet/tools"

#echo "🚀 Checking database connectivity..."
#until dotnet ef database update --no-build --verbose | grep -q "No migrations were applied"; do
#  >&2 echo "🟡 Waiting for database to be ready..."
#  sleep 1
#done
#
#echo "✅ Database connection successful."

# ✅ Check if there are pending migrations
PENDING_MIGRATIONS=$(dotnet ef migrations script --idempotent --output /dev/null)

if [[ -z "$PENDING_MIGRATIONS" ]]; then
  echo "✅ Database is already up to date. Skipping migrations."
  exit 0  # ✅ Exit successfully to mark container as completed
fi

# ✅ Apply pending migrations
echo "🚀 Applying pending migrations..."
dotnet ef database update --no-build

echo "✅ Migrations applied successfully!"
exit 0  # ✅ Ensure container exits successfully

