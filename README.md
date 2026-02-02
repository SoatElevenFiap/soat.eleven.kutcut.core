# KutCut Core API

Microserviço central da plataforma **KutCut** - sistema de processamento de vídeos desenvolvido como projeto da Pós-Graduação em **Arquitetura de Software** da FIAP.

## Sobre o Projeto

O KutCut é uma plataforma que permite aos usuários fazer upload de vídeos para extrair imagens em frames. Este repositório contém o **Core API**, responsável por:

- Upload de vídeos
- Listagem de vídeos do usuário
- Notificações ao usuário
- Comunicação via mensageria com outros serviços

## Arquitetura

O sistema segue uma arquitetura de **microserviços** com comunicação assíncrona via mensageria:

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Auth Service  │     │   Core API      │     │  Video Worker   │
│  (Autenticação) │     │  (Este Repo)    │     │ (Processamento) │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                          ┌──────┴──────┐
                          │  RabbitMQ   │
                          └─────────────┘
```

### Serviços Relacionados

| Serviço | Responsabilidade |
|---------|------------------|
| **Auth Service** | Gerenciamento de usuários e autenticação |
| **Core API** | Upload, listagem de vídeos e notificações |
| **Video Worker** | Processamento dos vídeos e extração de frames |

## Tecnologias

- **.NET 8** - Framework principal
- **ASP.NET Core Web API** - APIs REST
- **RabbitMQ** - Mensageria assíncrona
- **Swagger/OpenAPI** - Documentação da API
- **Clean Architecture** - Organização do projeto

## Estrutura do Projeto

```
├── soat.eleven.kutcut.core/          # API Layer (Controllers, Endpoints)
├── soat.eleven.kutcut.application/   # Application Layer (Use Cases, Services)
├── soat.eleven.kutcut.domain/        # Domain Layer (Entities, Interfaces)
└── soat.eleven.kutcut.infra/         # Infrastructure Layer (Repositories, External Services)
```

## Como Executar

```bash
# Restaurar dependências
dotnet restore

# Executar a aplicação
dotnet run --project soat.eleven.kutcut.core
```

A API estará disponível em `https://localhost:5001` com documentação Swagger em `/swagger`.

## Equipe

Projeto desenvolvido pelo **Grupo 11** - SOAT - Pós-Graduação FIAP

---

*Tech Challenge - Arquitetura de Software - FIAP 2025/2026*
