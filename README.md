UsersAPI microservice (Users only) - DDD skeleton with RabbitMQ publishing and JWT auth.

Run locally:
- dotnet run --project src/UsersAPI

Docker:
- docker build -t usersapi:latest -f src/UsersAPI/Dockerfile src/UsersAPI
- docker run -p 8080:80 --env-file .env usersapi:latest

Kubernetes:
- kubectl apply -f k8s/usersapi-config.yaml
- kubectl apply -f k8s/usersapi-secret.yaml
- kubectl apply -f k8s/usersapi-deployment.yaml

Notes:
- This service uses an in-memory user store and publishes `user.created` events to RabbitMQ.
- Secrets should be managed by your K8s secret store in production.
