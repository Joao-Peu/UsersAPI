# UsersAPI - Microserviço de Gerenciamento de Usuários

## ?? Quick Start

### Deploy no Kubernetes (COMPLETO)

```powershell
# 1. Navegar para a pasta do projeto
cd src/UsersAPI

# 2. Build da imagem Docker
docker build -t usersapi:latest .

# 3. Deploy no Kubernetes (ordem correta)
cd ../../k8s

# Secrets e ConfigMaps
kubectl apply -f usersapi-secret.yaml
kubectl apply -f usersapi-config.yaml

# UsersAPI
kubectl apply -f usersapi-deployment.yaml

# 4. Verificar status
kubectl get pods
kubectl logs -f deployment/usersapi

# 5. Testar Health Check
kubectl port-forward service/usersapi 8080:80
```

### ?? Deploy Automático

Use os comandos kubectl para deploy completo:

```powershell
# Deploy completo
kubectl apply -f k8s/

# Verificar logs
kubectl logs -f deployment/usersapi

# Acessar RabbitMQ Management UI (se estiver rodando)
kubectl port-forward service/rabbitmq 15672:15672
# Acesse: http://localhost:15672
# Login: fiap / fiap123
```

---

## ?? Endpoints

### Authentication & User Management

- **Health Check**: `GET http://localhost:8080/health`
- **Register User**: `POST http://localhost:8080/api/users/register`
- **Login**: `POST http://localhost:8080/api/auth/login`

### Exemplos de Requisições

#### Registrar Novo Usuário
```http
POST http://localhost:8080/api/users/register
Content-Type: application/json

{
  "name": "João Silva",
  "email": "joao@example.com",
  "password": "SenhaForte123!"
}
```

**Resposta (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "email": "joao@example.com"
}
```

#### Autenticar Usuário
```http
POST http://localhost:8080/api/auth/login
Content-Type: application/json

{
  "email": "joao@example.com",
  "password": "SenhaForte123!"
}
```

**Resposta (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

---

## ?? Configuração

### Variáveis de Ambiente
O .NET Configuration usa `__` (double underscore) para hierarquia de seções:

```sh
# RabbitMQ
RABBITMQ__HOSTNAME=rabbitmq
RABBITMQ__USERNAME=fiap
RABBITMQ__PASSWORD=fiap123

# JWT
JWT__KEY=c3VwZXJfc2VjcmV0X2tleV8xMjMh

# SQL Server (quando necessário)
CONNECTIONSTRINGS__USERDB=Server=localhost,14331;Database=UsersDb;User Id=sa;Password=StrongPassword!123;TrustServerCertificate=True;
```

### Mapeamento no Código

```csharp
// Program.cs lê assim:
builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>()
// ? RABBITMQ__HOSTNAME
// ? RABBITMQ__USERNAME
// ? RABBITMQ__PASSWORD

builder.Configuration["Jwt:Key"]
// ? JWT__KEY
```

---

## ?? Kubernetes

### Recursos Aplicados

```
# UsersAPI
Deployment: usersapi                (2 réplicas)
Service:    usersapi                (ClusterIP, porta 80)
ConfigMap:  usersapi-config         (RabbitMQ hostname)
Secret:     usersapi-secret         (Credenciais RabbitMQ + JWT Key)
```

### Portas Configuradas
- **UsersAPI Container**: 80
- **UsersAPI Service**: 80 (ClusterIP interno)
- **Port-forward API**: `kubectl port-forward service/usersapi 8080:80`

### Secrets (Base64)
```yaml
# Valores atuais em usersapi-secret.yaml
rabbitmq_user: ZmlhcA==          # fiap
rabbitmq_pass: ZmlhcDEyMw==      # fiap123
jwt_key: c3VwZXJfc2VjcmV0X2tleV8xMjMh  # super_secret_key_123!
```

---

## ?? RabbitMQ Integration

### Event Publishing

#### UserCreatedEvent
Quando um novo usuário é registrado, o evento `UserCreatedEvent` é publicado no RabbitMQ:

```csharp
// CreateUserHandler.cs
var @event = new UserCreatedEvent
{
    UserId = user.Id,
    Email = user.Email
};

await publisher.Publish(@event);
```

**Estrutura do Evento:**
```csharp
public class UserCreatedEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
}
```

### Configuração MassTransit

```csharp
// Program.cs
builder.Services.AddMassTransit(x =>
{
    var rabbitMQSettings = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>()!;
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMQSettings.HostName, "/", host =>
        {
            host.Username(rabbitMQSettings.UserName);
            host.Password(rabbitMQSettings.Password);
        });

        // Retry Policy: 5 tentativas com backoff exponencial
        cfg.UseMessageRetry(r =>
        {
            r.Exponential(5, TimeSpan.FromSeconds(3), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(3));
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

### Testar Publicação de Eventos

#### 1. Criar um Usuário
```sh
curl -X POST http://localhost:8080/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Teste User",
    "email": "teste@example.com",
    "password": "SenhaForte123!"
  }'
```

#### 2. Verificar no RabbitMQ Management UI
```sh
kubectl port-forward service/rabbitmq 15672:15672
# Acesse: http://localhost:15672
# Login: fiap / fiap123
# Vá em "Queues" e procure por mensagens publicadas
```

#### 3. Verificar Logs
```sh
kubectl logs -f deployment/usersapi
# Procure por logs de publicação de eventos
```

---

## ?? Autenticação JWT

### Geração de Token
- **Algoritmo**: HS256 (HMAC-SHA256)
- **Chave**: Configurada via `Jwt:Key` (appsettings.json ou Secret do K8s)
- **Claims**: `userId`, `email`, `role`

### Uso do Token
```http
GET http://localhost:8080/api/protected-endpoint
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Validação de Senha
Requisitos de senha (validados em `Password.IsPasswordValid`):
- ? Mínimo 8 caracteres
- ? Pelo menos 1 letra
- ? Pelo menos 1 número
- ? Pelo menos 1 caractere especial

---

## ??? Banco de Dados

### SQL Server Configuration
```csharp
// Program.cs - Configuração com retry automático
builder.Services.AddDbContext<UserDbContext>(options =>
{
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
});
```

### Entity: User
```csharp
public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public Password Password { get; private set; }
    public UserRole Role { get; private set; }
}
```

### Migrations
```sh
# Criar nova migration
dotnet ef migrations add MigrationName --project src/UsersAPI

# Aplicar migrations
dotnet ef database update --project src/UsersAPI
```

---

## ?? Testes

### Testar Health Check (Port-forward ativo)

```sh
# Health Check
curl http://localhost:8080/health

# Saída esperada:
# Healthy
```

### Testar Fluxo Completo

**Cenário 1: Registrar usuário e publicar evento**

```sh
# 1. Registrar usuário
curl -X POST http://localhost:8080/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Maria Silva",
    "email": "maria@example.com",
    "password": "SenhaSegura123!"
  }'

# Resposta esperada: 201 Created
# {
#   "id": "uuid-gerado",
#   "email": "maria@example.com"
# }

# 2. Verificar logs
kubectl logs -f deployment/usersapi
# Procurar por: "Publishing UserCreatedEvent..."

# 3. Verificar no RabbitMQ
# Acesse: http://localhost:15672
# Verifique se o evento foi publicado
```

**Cenário 2: Autenticar e obter token JWT**

```sh
# 1. Login
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "maria@example.com",
    "password": "SenhaSegura123!"
  }'

# Resposta esperada: 200 OK
# {
#   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
# }

# 2. Usar token para acessar endpoints protegidos
curl -X GET http://localhost:8080/api/protected-endpoint \
  -H "Authorization: Bearer <token-aqui>"
```

**Cenário 3: Validação de senha fraca**

```sh
# Tentar registrar com senha fraca
curl -X POST http://localhost:8080/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "test@example.com",
    "password": "123"
  }'

# Resposta esperada: 400 Bad Request
# {
#   "code": "invalid_password",
#   "message": "Senha deve ter no mínimo 8 caracteres, incluindo letras, números e caracteres especiais."
# }
```

---

## ?? Estrutura

```
UsersAPI/
??? src/UsersAPI/
?   ??? Application/
?   ?   ??? Abstractions/           # Result pattern
?   ?   ?   ??? Result.cs
?   ?   ?   ??? ResultT.cs
?   ?   ?   ??? Error.cs
?   ?   ??? Commands/
?   ?   ?   ??? CreateUser/
?   ?   ?   ?   ??? CreateUserCommand.cs
?   ?   ?   ?   ??? CreateUserHandler.cs
?   ?   ?   ??? AuthenticateUser/
?   ?   ?       ??? AuthenticateUserCommand.cs
?   ?   ?       ??? AuthenticateUserHandler.cs
?   ?   ??? DTOs/
?   ?   ?   ??? UserDto.cs
?   ?   ??? Interface/
?   ?   ?   ??? IPasswordService.cs
?   ?   ??? Services/
?   ?       ??? PasswordService.cs
?   ??? Controllers/
?   ?   ??? UsersController.cs      # POST /api/users/register
?   ?   ??? AuthController.cs       # POST /api/auth/login
?   ??? Domain/
?   ?   ??? Entities/
?   ?   ?   ??? User.cs
?   ?   ??? Enums/
?   ?   ?   ??? UserRole.cs
?   ?   ??? ValueObjects/
?   ?       ??? Password.cs
?   ??? Infrastructure/
?   ?   ??? Interfaces/
?   ?   ?   ??? IUserRepository.cs
?   ?   ??? Persistence/
?   ?   ?   ??? UserConfiguration.cs
?   ?   ??? RabbitMQ/
?   ?   ?   ??? RabbitMQSettings.cs
?   ?   ??? UserDbContext.cs
?   ?   ??? UserRepository.cs
?   ??? Migrations/                 # Entity Framework migrations
?   ??? Shared/
?   ?   ??? Events/
?   ?       ??? UserCreatedEvent.cs
?   ??? Dockerfile                  # Multi-stage build
?   ??? Program.cs                  # Configuração da aplicação
?   ??? appsettings.json            # Configurações locais
?   ??? UsersAPI.csproj
??? k8s/
?   ??? usersapi-secret.yaml        # Secrets (RabbitMQ + JWT)
?   ??? usersapi-config.yaml        # ConfigMap (RabbitMQ host)
?   ??? usersapi-deployment.yaml    # Deployment + Service
??? README.md
```

---

## ?? Troubleshooting

### Pod não inicia (CrashLoopBackOff)

```sh
# Ver logs detalhados
kubectl logs -f deployment/usersapi

# Verificar eventos
kubectl describe pod -l app=usersapi

# Problemas comuns:
# 1. RabbitMQ não conecta ? Verifique: kubectl get pods | grep rabbitmq
# 2. Banco de dados não conecta ? Verifique connection string
# 3. Secrets não configurados ? Verifique: kubectl get secrets usersapi-secret
# 4. Imagem não encontrada ? Verifique: docker images | grep usersapi
```

### Erro: Cannot connect to RabbitMQ

**Causa**: UsersAPI inicia antes do RabbitMQ estar pronto

**Solução:**

```sh
# 1. Verificar se RabbitMQ está running
kubectl get pods -l app=rabbitmq

# 2. Aguardar RabbitMQ ficar pronto
kubectl wait --for=condition=ready pod -l app=rabbitmq --timeout=60s

# 3. Reiniciar UsersAPI
kubectl delete pod -l app=usersapi
```

### Erro: Connection string 'UserDb' não configurada

**Causa**: Variável de ambiente não configurada no deployment

**Solução:**
Adicione a connection string no `usersapi-deployment.yaml`:

```yaml
env:
  - name: ConnectionStrings__UserDb
    value: "Server=sqlserver;Database=UsersDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
```

### Senha não é aceita no registro

**Causa**: Senha não atende aos requisitos de segurança

**Requisitos:**
- ? Mínimo 8 caracteres
- ? Pelo menos 1 letra (maiúscula ou minúscula)
- ? Pelo menos 1 número (0-9)
- ? Pelo menos 1 caractere especial (!@#$%^&*()_+-=[]{}|;:,.<>?)

**Exemplo de senha válida:** `SenhaForte123!`

### Verificar Status Geral

```sh
# Status de todos os pods
kubectl get pods

# Logs UsersAPI
kubectl logs -f deployment/usersapi

# Verificar secrets
kubectl get secrets usersapi-secret -o yaml

# Verificar configmap
kubectl get configmap usersapi-config -o yaml

# Remover tudo e redeployar
kubectl delete -f k8s/
kubectl apply -f k8s/usersapi-secret.yaml
kubectl apply -f k8s/usersapi-config.yaml
kubectl apply -f k8s/usersapi-deployment.yaml
```

---

## ?? Segurança

- ? Senhas armazenadas com hash (BCrypt via `PasswordService`)
- ? Autenticação JWT com chave configurável
- ? Validação de senha forte (mínimo 8 caracteres, letras, números, especiais)
- ? Configurações separadas do código (Secrets do K8s)
- ?? Credenciais RabbitMQ são demo - **MUDE EM PRODUÇÃO**
- ?? JWT Key padrão - **Configure uma chave forte em produção**
- ?? `RequireHttpsMetadata = false` - **Habilite HTTPS em produção**

### Alterar Credenciais RabbitMQ

```sh
# 1. Codificar novas credenciais em base64
echo -n "novo_usuario" | base64
echo -n "nova_senha" | base64

# 2. Editar k8s/usersapi-secret.yaml
# Substituir valores de rabbitmq_user e rabbitmq_pass

# 3. Reaplicar secret
kubectl apply -f k8s/usersapi-secret.yaml

# 4. Reiniciar pods
kubectl delete pod -l app=usersapi
```

---

## ?? Notas Técnicas

### MassTransit + RabbitMQ
- ? Usa MassTransit 8.0 para abstração do RabbitMQ
- ? Publisher: Publica `UserCreatedEvent` após registro de usuário
- ? Retry Policy: Exponential backoff (5 tentativas, 3s ? 2min)
- ? Integração assíncrona com outros microserviços (ex: NotificationsAPI)

### Entity Framework Core
- ? Code-First approach com Migrations
- ? Retry automático em falhas de conexão (5 tentativas, max 10s delay)
- ? Configuração via `UserConfiguration` (Fluent API)
- ? SQL Server como database provider

### Result Pattern
Implementação de Result pattern para tratamento de erros:

```csharp
// Sucesso
Result<UserDto>.Success(userDto);

// Falha
Result<UserDto>.Failure(new Error("error_code", "Error message"));

// Verificação
if (result.IsFailure)
{
    return BadRequest(result.Error);
}
```

### Password Hashing
- ? BCrypt para hash de senhas
- ? Salt automático por senha
- ? Validação de requisitos de complexidade

---

## ?? Melhorias para Produção

### Health Checks Avançados
Adicione health checks para RabbitMQ e SQL Server:

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddRabbitMQ(rabbitConnectionString: $"amqp://{rabbitMQSettings.UserName}:{rabbitMQSettings.Password}@{rabbitMQSettings.HostName}")
    .AddSqlServer(connectionString);

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
```

Habilite health checks no `usersapi-deployment.yaml`:

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 80
  initialDelaySeconds: 10
  periodSeconds: 5
```

### Secrets Management
- Use Azure Key Vault, AWS Secrets Manager ou HashiCorp Vault
- Implemente rotação automática de JWT keys
- Configure managed identities para acesso ao banco

### Observabilidade
- Adicione Application Insights ou Prometheus
- Configure logs estruturados (Serilog)
- Implemente tracing distribuído (OpenTelemetry)
- Monitore métricas de autenticação e registro

### API Documentation
Adicione Swagger/OpenAPI:

```csharp
// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ...

app.UseSwagger();
app.UseSwaggerUI();
```

### Rate Limiting
Proteja endpoints contra abuso:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
```

### Email Verification
Implemente verificação de email no registro:

```csharp
// Após registro, enviar email de verificação
var verificationToken = GenerateVerificationToken(user.Id);
await _emailService.SendVerificationEmailAsync(user.Email, verificationToken);

// Endpoint para verificar email
[HttpGet("verify-email")]
public async Task<IActionResult> VerifyEmail([FromQuery] string token)
{
    // Validar token e ativar usuário
}
```

### Refresh Tokens
Implemente refresh tokens para renovação de JWT:

```csharp
public class RefreshTokenCommand
{
    public string RefreshToken { get; set; }
}

// Endpoint para renovar token
[HttpPost("refresh")]
public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
{
    var newToken = await _tokenService.RefreshTokenAsync(command.RefreshToken);
    return Ok(new { token = newToken });
}
```

---

## ?? Referências

- [ASP.NET Core Web API](https://learn.microsoft.com/aspnet/core/web-api/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [MassTransit Documentation](https://masstransit.io/)
- [JWT Authentication](https://jwt.io/)
- [Kubernetes Best Practices](https://kubernetes.io/docs/concepts/configuration/overview/)

---

## ?? Licença

Este projeto é parte da Pós-Graduação FIAP e é fornecido como material educacional.

---

## ?? Contribuidores

- **Repositório Original**: [Joao-Peu/UsersAPI](https://github.com/Joao-Peu/UsersAPI)
- **Pós-Graduação FIAP** - Arquitetura de Microsserviços
