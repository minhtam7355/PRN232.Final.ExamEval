#!/bin/bash

# Script để clean và rebuild solution một cách đáng tin cậy
# Giải quyết vấn đề NuGet restore không ổn định

echo "🧹 Cleaning solution..."
dotnet clean --verbosity quiet

echo "🗑️  Removing bin/obj folders..."
find . -type d -name "bin" -o -name "obj" | xargs rm -rf

echo "📦 Clearing NuGet cache for this solution..."
dotnet nuget locals temp-cache --clear
dotnet nuget locals http-cache --clear

echo "🔄 Restoring NuGet packages..."
dotnet restore --force --no-cache

echo "🔨 Building solution..."
dotnet build --no-restore

echo "✅ Done!"

