#!/bin/bash

set -e

# ==========================================
# Configuration
# ==========================================

BACKEND_IMAGE="backend:dev"
FRONTEND_IMAGE="frontend:dev"

BACKEND_CONTAINER="backend-dev"
FRONTEND_CONTAINER="frontend-dev"

BACKEND_PORT=8080
FRONTEND_PORT=3000


# ==========================================
# Helper functions
# ==========================================

print_step() {
    echo ""
    echo "=========================================="
    echo "$1"
    echo "=========================================="
}


# ==========================================
# 1. Stop and remove existing containers
# ==========================================

print_step "Stopping existing project containers"

for CONTAINER in "$BACKEND_CONTAINER" "$FRONTEND_CONTAINER"; do

    if docker ps -q -f "name=^${CONTAINER}$" | grep -q .; then
        echo "Stopping running container: $CONTAINER"
        docker stop "$CONTAINER"
    else
        echo "Container $CONTAINER is not running"
    fi

done


print_step "Removing existing project containers"

for CONTAINER in "$BACKEND_CONTAINER" "$FRONTEND_CONTAINER"; do

    if docker ps -aq -f "name=^${CONTAINER}$" | grep -q .; then
        echo "Removing container: $CONTAINER"
        docker rm "$CONTAINER"
    else
        echo "Container $CONTAINER does not exist"
    fi

done


# ==========================================
# 2. Build Backend
# ==========================================

print_step "Building Backend Docker image"

docker build \
    -t "$BACKEND_IMAGE" \
    ./backend


# ==========================================
# 3. Build Frontend
# ==========================================

print_step "Building Frontend Docker image"

docker build \
    -t "$FRONTEND_IMAGE" \
    ./frontend


# ==========================================
# 4. Run Backend
# ==========================================

print_step "Starting Backend container"

docker run -d \
    --name "$BACKEND_CONTAINER" \
    -p "$BACKEND_PORT:8080" \
    -e ASPNETCORE_ENVIRONMENT=Development \
    "$BACKEND_IMAGE"


# ==========================================
# 5. Run Frontend
# ==========================================

print_step "Starting Frontend container"

docker run -d \
    --name "$FRONTEND_CONTAINER" \
    -p "$FRONTEND_PORT:80" \
    "$FRONTEND_IMAGE"


# ==========================================
# 6. Show running containers
# ==========================================

print_step "Running containers"

docker ps


# ==========================================
# 7. Open browser
# ==========================================

print_step "Opening applications in browser"

if command -v xdg-open > /dev/null; then

    xdg-open "http://localhost:3000" > /dev/null 2>&1 &
    xdg-open "http://localhost:8080/weatherforecast" > /dev/null 2>&1 &

elif command -v firefox > /dev/null; then

    firefox "http://localhost:3000" "http://localhost:8080/weatherforecast" > /dev/null 2>&1 &

else

    echo "Could not automatically open browser."
    echo "Frontend: http://localhost:3000"
    echo "Backend:  http://localhost:8080/weatherforecast"

fi


# ==========================================
# Done
# ==========================================

print_step "Deployment completed"

echo "Frontend: http://localhost:3000"
echo "Backend:  http://localhost:8080/weatherforecast"
echo ""
echo "Containers:"
echo "  - $FRONTEND_CONTAINER"
echo "  - $BACKEND_CONTAINER"
echo ""