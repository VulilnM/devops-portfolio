#!/bin/bash

set -e

echo "========================================"
echo " DevOps Portfolio - Build & Run"
echo "========================================"

cd "$(dirname "$0")"

# ----------------------------------------
# 1. Generate EF migration if missing
# ----------------------------------------

echo ""
echo "[1/4] Checking EF Core migrations..."

if [ ! -d "./backend/Migrations" ]; then

    echo "Migrations directory does not exist."
    echo "Generating InitialCreate migration..."

    cd backend

    dotnet ef migrations add InitialCreate

    cd ..

    echo "Migration generated successfully."

else

    echo "Migrations directory already exists."
    echo "Skipping migration generation."

fi


# ----------------------------------------
# 2. Stop and remove existing containers
# ----------------------------------------

echo ""
echo "[2/4] Stopping existing containers..."

docker compose down

echo "Existing containers stopped."


# ----------------------------------------
# 3. Build and start application
# ----------------------------------------

echo ""
echo "[3/4] Building and starting containers..."

docker compose up --build -d

echo "Containers started."


# ----------------------------------------
# 4. Open frontend and backend
# ----------------------------------------

echo ""
echo "[4/4] Opening application..."

sleep 3

xdg-open http://localhost:3000 >/dev/null 2>&1 &
xdg-open http://localhost:8080/swagger >/dev/null 2>&1 &

echo ""
echo "========================================"
echo " Application is running!"
echo "========================================"
echo ""
echo "Frontend : http://localhost:3000"
echo "Backend  : http://localhost:8080"
echo "Swagger  : http://localhost:8080/swagger"
echo ""