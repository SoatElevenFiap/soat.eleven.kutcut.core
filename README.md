# Soat Eleven Kutcut Core API

Microserviço central da plataforma KutCut, responsável pelo ciclo de vida dos vídeos (upload, consulta, atualização, exclusão), integração assíncrona via mensageria e fluxo de notificações.

## 📋 Índice

- [Sobre o Projeto](#-sobre-o-projeto)
- [Tecnologias Utilizadas](#-tecnologias-utilizadas)
- [Arquitetura](#-arquitetura)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Pré-requisitos](#-pré-requisitos)
- [Configuração e Execução](#-configuração-e-execução)
- [Containerização e Deploy](#-containerização-e-deploy)
- [Endpoints da API](#-endpoints-da-api)
- [Testes](#-testes)
- [Variáveis de Configuração](#-variáveis-de-configuração)
- [Dependências Principais](#-dependências-principais)
- [Equipe](#-equipe)
- [Licença](#-licença)

## 🎯 Sobre o Projeto

O KutCut é uma plataforma para processamento de vídeos e extração de frames. Este repositório contém o Core API, que concentra as responsabilidades de:

- Upload de vídeos com metadados
- Listagem paginada e consulta por status
- Consulta de detalhes de vídeo
- Atualização parcial de título
- Download de thumbnails em `.zip`
- Exclusão de vídeos
- Publicação/consumo de eventos via RabbitMQ
- Processamento de notificações em background
- Integração com serviço de autenticação de usuários

### Visão arquitetural

<img width="1565" height="1070" alt="Arquitetura KutCut" src="https://github.com/user-attachments/assets/09a4999a-5a65-42e2-b61c-0a708497e386" />

## 🚀 Tecnologias Utilizadas

- **.NET 8.0** - Framework principal
- **ASP.NET Core Web API** - Exposição de endpoints REST
- **Entity Framework Core 8.0** - ORM para acesso a dados
- **PostgreSQL** - Banco de dados relacional
- **RabbitMQ** - Mensageria assíncrona
- **JWT Bearer** - Autenticação e autorização
- **Azure Blob Storage** - Armazenamento de arquivos
- **MailKit** - Envio de e-mails
- **Asp.Versioning** - Versionamento da API
- **Swagger/OpenAPI** - Documentação da API
- **Dockerfile, Docker & Docker Compose** - Containerização
- **Helm** - Empacotamento e deploy no Kubernetes
- **xUnit** - Framework de testes
- **Moq** - Mock para testes
- **Coverlet** - Cobertura de código

## 🏗️ Arquitetura

O projeto segue os princípios de **Clean Architecture** e está organizado em camadas:

### 1. **API Layer** (`soat.eleven.kutcut.core`)
- Ponto de entrada da aplicação
- Controllers versionados (`/api/v1/...`)
- Middlewares, autenticação JWT e documentação Swagger

### 2. **Application Layer** (`soat.eleven.kutcut.application`)
- Casos de uso e orquestração de regras de negócio
- DTOs de entrada e saída
- Serviços de aplicação e processadores de notificação

### 3. **Domain Layer** (`soat.eleven.kutcut.domain`)
- Entidades de domínio
- Enums, contratos e regras de negócio centrais

### 4. **Infrastructure Layer** (`soat.eleven.kutcut.infra`)
- Contexto EF Core
- Repositórios e serviços externos (storage, e-mail, etc.)

### 5. **Infra Queues Layer** (`soat.eleven.kutcut.infra.queues`)
- Conexão com RabbitMQ
- Publicadores e listeners de mensagens

### Serviços Relacionados

| Serviço | Responsabilidade |
|---------|------------------|
| **Auth Service** | Gerenciamento de usuários e autenticação |
| **Core API** | Upload, listagem, consulta e notificações de vídeos |
| **Video Worker** | Processamento dos vídeos e extração de frames |

## 📁 Estrutura do Projeto

```text
.
├── Dockerfile
├── README.md
├── helm/
│   ├── Chart.yaml
│   ├── values.yaml
│   └── templates/
├── infra-local/
│   └── docker-compose.yml
├── rabit-local/
│   ├── processed-message.json
│   ├── rabbit-local.config.json
│   └── uploaded-message.json
├── soat.eleven.kutcut.core/
│   ├── soat.eleven.kutcut.core.sln
│   ├── soat.eleven.kutcut.application/
│   ├── soat.eleven.kutcut.core/
│   ├── soat.eleven.kutcut.domain/
│   ├── soat.eleven.kutcut.infra/
│   └── soat.eleven.kutcut.infra.queues/
└── tests/
		└── soat.eleven.kutcut.tests/
```

## 📦 Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) e [Docker Compose](https://docs.docker.com/compose/)
- [PostgreSQL 16+](https://www.postgresql.org/) (ou via Docker)
- [RabbitMQ](https://www.rabbitmq.com/) (ou via Docker)

## ⚙️ Configuração e Execução

### 1. Restaurar dependências

```bash
dotnet restore .\soat.eleven.kutcut.core\soat.eleven.kutcut.core.sln
```

### 2. Subir infraestrutura local (PostgreSQL + RabbitMQ)

```bash
docker compose -f .\infra-local\docker-compose.yml up -d
```

Isso irá iniciar:
- PostgreSQL na porta `5432`
- RabbitMQ AMQP na porta `5672`
- RabbitMQ Management UI na porta `15672`

### 3. Executar migrations (se necessário)

```bash
dotnet ef database update --project .\soat.eleven.kutcut.core\soat.eleven.kutcut.infra\soat.eleven.kutcut.infra.csproj --startup-project .\soat.eleven.kutcut.core\soat.eleven.kutcut.core\soat.eleven.kutcut.core.api.csproj
```

### 4. Executar a API

```bash
dotnet run --project .\soat.eleven.kutcut.core\soat.eleven.kutcut.core\soat.eleven.kutcut.core.api.csproj
```

Swagger em ambiente de desenvolvimento:
- `https://localhost:<porta>/swagger`

## 📦 Containerização e Deploy

Este projeto utiliza:
- **Dockerfile** para build e execução da API em container
- **Docker Compose** em `infra-local/docker-compose.yml` para infraestrutura local
- **Helm** (chart em `helm/`) para deploy no Kubernetes

### Build da imagem com Dockerfile

```bash
docker build -f Dockerfile -t kutcut-core-api:latest .
```

### Exemplo de deploy com Helm

```bash
helm upgrade --install kutcut-api ./helm -f ./helm/values.yaml
```

> Observação: os valores sensíveis (`secret.*`) devem ser informados via `--set` ou secret manager no pipeline de deploy.

## 🔌 Endpoints da API

Base route: `/api/v1/videos`

> Todos os endpoints exigem autenticação JWT (`Authorization: Bearer <token>`).

### Vídeos

#### `POST /api/v1/videos`
Upload de vídeo com metadados (`multipart/form-data`).

#### `GET /api/v1/videos`
Listagem paginada de vídeos.

#### `GET /api/v1/videos/{id}`
Busca vídeo por ID.

#### `GET /api/v1/videos/status/{status}`
Filtra vídeos por status com paginação.

#### `PATCH /api/v1/videos/{id}`
Atualiza parcialmente o título do vídeo.

#### `GET /api/v1/videos/{id}/thumbnails/download`
Download do arquivo `.zip` de thumbnails.

#### `DELETE /api/v1/videos/{id}`
Remove um vídeo por ID.

## 🧪 Testes

O projeto possui testes unitários e de integração no projeto `tests/soat.eleven.kutcut.tests`.

### Executar todos os testes

```bash
dotnet test .\tests\soat.eleven.kutcut.tests\soat.eleven.kutcut.tests.csproj
```

### Executar testes com cobertura

```bash
dotnet test .\tests\soat.eleven.kutcut.tests\soat.eleven.kutcut.tests.csproj /p:CollectCoverage=true
```

## 🔧 Variáveis de Configuração

As configurações principais estão em:
- `soat.eleven.kutcut.core/soat.eleven.kutcut.core/appsettings.json`
- `soat.eleven.kutcut.core/soat.eleven.kutcut.core/appsettings.Development.json`

### appsettings (seções principais)

```json
{
	"ConnectionStrings": {
		"PostgresConnectionString": "Host=localhost;Port=5432;Database=kutcut;Username=postgres;Password=***"
	},
	"JwtSettings": {
		"SecretKey": "***"
	},
	"RabbitMQ": {
		"HostName": "localhost",
		"Port": 5672,
		"UserName": "***",
		"Password": "***",
		"VideoProcessingQueueName": "processamento_de_videos",
		"VideoUploadedQueueName": "video_uploaded"
	},
	"EmailSettings": {
		"SmtpServer": "smtp.gmail.com",
		"Port": 587,
		"FromEmail": "kutcut.videos@gmail.com",
		"FromName": "KutCut Platform",
		"UserName": "***",
		"Password": "***"
	},
	"AuthService": {
		"BaseUrl": "http://localhost:5208",
		"TimeoutSeconds": 30
	},
	"AzureBlobStorage": {
		"ConnectionString": "***",
		"ContainerName": "kutcut"
	}
}
```

### Variáveis do Docker Compose local

- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `RABBITMQ_DEFAULT_USER`
- `RABBITMQ_DEFAULT_PASS`

## 📝 Dependências Principais

### API
- Microsoft.AspNetCore.Authentication.JwtBearer
- Asp.Versioning.Mvc
- Asp.Versioning.Mvc.ApiExplorer
- Microsoft.EntityFrameworkCore.Design
- Swashbuckle.AspNetCore
- Azure.Extensions.AspNetCore.Configuration.Secrets
- Azure.Identity

### Application
- FluentResults
- Microsoft.Extensions.Hosting.Abstractions
- Microsoft.Extensions.Options

### Infrastructure
- Microsoft.EntityFrameworkCore
- Npgsql.EntityFrameworkCore.PostgreSQL
- Azure.Storage.Blobs
- MailKit

### Infra Queues
- RabbitMQ.Client

### Tests
- xUnit
- Moq
- FluentAssertions
- coverlet.collector
- coverlet.msbuild

## 👥 Equipe

Projeto desenvolvido pelo **Grupo 11** - SOAT - Pós-Graduação FIAP

- **Adriano Ricardo Felippe Torini**
- **André Luiz**
- **Dhyogo Siqueira**
- **Filipe Braga**
- **Kauan Kajitani**

## 📄 Licença

Este projeto é privado e pertence à equipe SOAT Eleven.

---

*Tech Challenge - Arquitetura de Software - FIAP 2025/2026*
