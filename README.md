# 🔨 Leilão Tempo Real (High Performance Auction)

Sistema de leilão em tempo real projetado para **alta concorrência** e **resiliência**. O projeto utiliza uma arquitetura orientada a eventos (Event-Driven) para garantir que lances sejam processados em milissegundos e persistidos com segurança, mesmo em caso de falhas críticas.

![Badge .NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=flat&logo=dotnet&logoColor=white)
![Badge Angular](https://img.shields.io/badge/Angular-DD0031?style=flat&logo=angular&logoColor=white)
![Badge Redis](https://img.shields.io/badge/Redis-DC382D?style=flat&logo=redis&logoColor=white)
![Badge RabbitMQ](https://img.shields.io/badge/RabbitMQ-FF6600?style=flat&logo=rabbitmq&logoColor=white)
![Badge SignalR](https://img.shields.io/badge/SignalR-Realtime-blue)

## 🚀 Arquitetura e Fluxo de Dados

O sistema resolve o problema clássico de **race condition** (condição de corrida) em leilões disputados e garante **Zero Data Loss**.

### 🔄 Fluxo do Lance

1. **Entrada:** O usuário envia um lance via API.
2. **Validação Atômica (Redis):** Um **script Lua** roda no Redis para garantir atomicidade. Ele verifica se o leilão está ativo e se o valor é maior que o atual.  
   *Resultado:* O usuário recebe feedback em milissegundos (sucesso ou **Lance Baixo**).
3. **Real-time (SignalR):** Se aceito, o novo valor é notificado instantaneamente via WebSocket para todos os conectados.
4. **Durabilidade (RabbitMQ):** Um evento `LanceCriadoEvent` é publicado no barramento de mensagens.
5. **Persistência Assíncrona (RabbitMQ):** Um consumer (`LanceCriadoConsumer`) lê a fila e salva a transação no banco de dados (SQL Server).

> **Destaque:** Se a API cair após o passo 4, o RabbitMQ retém a mensagem. Quando o servidor voltar, o lance é processado. **Nenhum dado é perdido.**

## 🛠️ Tecnologias Utilizadas

### Back-end (.NET 8)

- **ASP.NET Core Web API:** Entrada de dados.
- **MassTransit:** Abstração para mensageria (RabbitMQ).
- **SignalR:** Comunicação WebSocket em tempo real.
- **StackExchange.Redis:** Comunicação com cache distribuído.
- **Entity Framework Core:** ORM para SQL Server.
- **Moq & xUnit:** Testes unitários com mocks.

### Front-end (Angular)

- **Angular 17+ (Standalone Components):** Estrutura moderna sem NgModules.
- **RxJS:** Manipulação reativa de eventos.
- **SignalR Client:** Conexão com o Hub.

### Infraestrutura (Docker)

- **Redis:** Gerenciamento de estado volátil e locking.
- **RabbitMQ:** Message broker para desacoplamento e durabilidade.
- **SQL Server:** Banco de dados relacional (persistência definitiva).

## ⚙️ Como Rodar o Projeto

### Pré-requisitos

- Docker e Docker Compose
- .NET 8 SDK
- Node.js (v18+) e Angular CLI

### 1️⃣ Subir a Infraestrutura

Na raiz do projeto (onde está o `docker-compose.yml`), execute:

```bash
docker-compose up -d
```

### 2️⃣ Configurar o Back-end

Navegue até a pasta da API.

Configure a connection string no `appsettings.json`, via User Secrets ou arquivo `.env`.

Execute as migrations (se houver) ou deixe o EF Core criar o banco.

Rode a API:

```bash
cd LeilaoTempoReal.API
dotnet run
```

A API estará rodando em `https://localhost:7101` (ou porta configurada).

### 3️⃣ Rodar o Front-end

Navegue até a pasta do Angular.

Instale as dependências:

```bash
npm install
```

Rode o servidor de desenvolvimento:

```bash
ng serve
```

Acesse `http://localhost:4200`.

## 🧪 Testes

O projeto conta com testes unitários cobrindo regras de negócio críticas (validação de lances, expiração de tempo e rejeição no Redis).

Para rodar os testes:

```bash
dotnet test
```

## 📂 Estrutura do Projeto (Clean Architecture Simplificada)

- `src/API`: Controllers, Hubs, Consumers e configurações.
- `src/Application`: Regras de negócio, services e definição de eventos.
- `src/Domain`: Entidades e interfaces.
- `src/Infrastructure`: Contexto do banco, repositórios e scripts Lua.
- `tests`: Testes unitários com xUnit.

## 🛡️ Tratamento de Falhas (Resiliência)

- **Redis fora do ar?** O sistema trata a falha de conexão e evita inconsistência.
- **API crash?** Mensagens não processadas ficam em estado `Ready` no RabbitMQ e são retomadas automaticamente no reinício (graceful recovery).
- **Lance rejeitado?** O front-end exibe o valor atualizado em tempo real caso o usuário tente um lance menor que o último registrado no servidor.

---

Desenvolvido como **estudo de caso** para arquiteturas de **alta performance** em .NET.
