
`cd infra && docker compose up --build -d`

`docker exec examprep-kafka kafka-console-consumer --bootstrap-server localhost:9092 --topic user-registered --from-beginning --max-messages 1`