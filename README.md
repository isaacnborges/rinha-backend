# rinha-backend
Projeto em c# com dotnet 7 utilizando minimal api para participar da rinha de backend. Mais detalhes no repositório [rinha-de-backend-2023-q3](https://github.com/zanfranceschi/rinha-de-backend-2023-q3)

### Tecnologias
- [C# 11](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- [Docker](https://www.docker.com/) / Docker compose (container orchestrator)
- [Redis](https://redis.io/) (Distributed Cache)
- [Nginx](https://www.nginx.com/) (Load balancer)
- [Postgres](https://www.postgresql.org/) (Database)
- [Dapper](https://github.com/DapperLib/Dapper)
- [Swagger](https://swagger.io/)
- [FluentValidation](https://docs.fluentvalidation.net/en/latest/)

### Requisitos
  - [Docker](https://docs.docker.com/engine/install/)

### Compilar e executar o projeto
Para compilar e executar o projeto, siga as instruções abaixo:
1. Necessário possuir a versão [.Net 7](https://dotnet.microsoft.com/download/dotnet/7.0) instalada.
2. No terminal, navegue até o diretório raiz do projeto.
3. Execute o seguinte comando para executar o projeto:
```
dotnet run --project .\src\WebApi.csproj
```