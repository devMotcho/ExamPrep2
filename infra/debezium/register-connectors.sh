#!/bin/sh
set -e

for config in /connector-configs/*.json; do
  name=$(basename "$config" .json)
  status_code=$(curl -s -o /dev/null -w "%{http_code}" http://debezium:8083/connectors/"$name")
  if [ "$status_code" = "200" ]; then
    echo "$name already registered, skipping"
  else
    echo "Registering $name..."
    curl -s -X POST http://debezium:8083/connectors \
      -H "Content-Type: application/json" \
      -d @"$config"
  fi
done