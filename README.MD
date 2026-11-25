# Habitarium API

API REST do **Habitarium**, aplicativo Android de orçamento de materiais de construção. Ela fornece o catálogo de produtos e a autenticação dos usuários, e é consumida pelo [app Android](https://github.com/Arthur-Cardoso0/Projeto-Mobile) via Retrofit.

**API publicada:** `https://api-habitarium-biho.onrender.com/swagger`

> A API roda no plano gratuito do Render, que hiberna quando fica um tempo sem receber requisições. A primeira chamada depois de um período parado pode levar cerca de um minuto.

## Sumário

- [Tecnologias](#tecnologias)
- [Funcionalidades](#funcionalidades)
- [Pré-requisitos](#pré-requisitos)
- [Como executar localmente](#como-executar-localmente)
- [Endpoints](#endpoints)
- [Exemplos de uso](#exemplos-de-uso)
- [Modelo de dados](#modelo-de-dados)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Publicação na nuvem](#publicação-na-nuvem)
- [Segurança](#segurança)
- [Integração com o app Android](#integração-com-o-app-android)
- [Próximos passos](#próximos-passos)
- [Autor](#autor)

## Tecnologias

| Tecnologia | Uso |
|---|---|
| .NET 10 / ASP.NET Core Web API | Base da aplicação |
| Entity Framework Core 10 | Acesso a dados e migrações |
| PostgreSQL + Npgsql | Banco de dados (Neon em produção) |
| JWT Bearer | Autenticação |
| `PasswordHasher` (ASP.NET Core) | Hash de senhas |
| Swashbuckle (Swagger) | Documentação e testes interativos em desenvolvimento |
| Docker | Empacotamento para publicação |
| Render + Neon | Hospedagem da API e do banco |

## Funcionalidades

- Catálogo de materiais com empresa, marca, avaliação, estoque, indicação de patrocinado, instalação disponível e especificações técnicas.
- Cadastro de materiais em lote.
- Cadastro e login de usuários com e-mail e senha.
- Token JWT com validade de 24 horas.
- Leitura do catálogo pública; escrita (cadastrar e apagar produtos) restrita ao papel **admin**.
- Migrações aplicadas automaticamente ao iniciar a API.
- Swagger com botão **Authorize**, disponível apenas no ambiente de desenvolvimento.

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Um banco PostgreSQL: local ([download](https://www.postgresql.org/download/)) ou hospedado, como o [Neon](https://neon.tech)
- Ferramenta do EF Core (opcional, só para criar novas migrações):

```powershell
dotnet tool install --global dotnet-ef
```

## Como executar localmente

**1.** Clone o repositório e entre na pasta:

```powershell
git clone https://github.com/Arthur-Cardoso0/API-HABITARIUM.git
cd API-HABITARIUM
```

**2.** Configure os segredos. Eles ficam fora do código, no *user-secrets* do .NET:

```powershell
dotnet user-secrets init

# Conexão com o PostgreSQL
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=HabitariumDb;Username=postgres;Password=SUA_SENHA"

# Chave que assina os tokens JWT (gera 48 bytes aleatórios)
$b = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($b))

# E-mail(s) com permissão de administrador (separados por vírgula)
dotnet user-secrets set "Admin:Emails" "seu-email@exemplo.com"
```

**3.** Execute a API:

```powershell
dotnet run --launch-profile http
```

Ao iniciar, a API aplica as migrações no banco configurado. Ela fica disponível em `http://localhost:5240`.

**4.** Abra o Swagger em `http://localhost:5240/swagger`.

### Configurações

Em produção, os nomes viram variáveis de ambiente com **dois sublinhados** no lugar dos dois pontos (por exemplo, `Jwt__Key`).

| Chave | Descrição | Onde fica |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Conexão com o PostgreSQL (obrigatória) | user-secrets ou variável de ambiente |
| `Jwt:Key` | Chave de assinatura do token (obrigatória, mínimo de 32 caracteres) | user-secrets ou variável de ambiente |
| `Admin:Emails` | E-mails com papel de administrador, separados por vírgula | user-secrets ou variável de ambiente |
| `Jwt:Issuer` | Emissor do token | `appsettings.json` |
| `Jwt:Audience` | Público do token | `appsettings.json` |
| `Jwt:ExpiresMinutes` | Validade do token em minutos (padrão 1440) | `appsettings.json` |

## Endpoints

Base local: `http://localhost:5240/api`. Base publicada: `https://api-habitarium-biho.onrender.com/api`.

### Autenticação

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| POST | `/auth/register` | Público | Cria a conta e devolve o token |
| POST | `/auth/login` | Público | Valida e-mail e senha e devolve o token |
| GET | `/auth/me` | Autenticado | Devolve os dados do usuário do token |

Regras do cadastro: nome de 2 a 80 caracteres, e-mail válido e senha de 8 a 100 caracteres. E-mail repetido retorna `409`; credenciais inválidas retornam `401`.

### Materiais

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | `/materials` | Público | Lista todos os materiais |
| GET | `/materials/{id}` | Público | Busca um material pelo GUID |
| POST | `/materials` | Admin | Cadastra um material |
| POST | `/materials/batch` | Admin | Cadastra vários materiais de uma vez |
| DELETE | `/materials/{id}` | Admin | Remove um material |
| DELETE | `/materials/bulk` | Admin | Remove vários materiais (lista de GUIDs) |

Usuário comum autenticado que tentar escrever no catálogo recebe `403`; sem token, `401`.

## Exemplos de uso

Os exemplos usam PowerShell. Troque `$base` pela URL local se estiver rodando na sua máquina.

```powershell
$base = "https://api-habitarium-biho.onrender.com/api"

# Listar o catálogo (público)
Invoke-RestMethod "$base/materials"

# Cadastrar um usuário
Invoke-RestMethod "$base/auth/register" -Method Post -ContentType "application/json" `
  -Body '{"name":"Maria","email":"maria@exemplo.com","password":"Senha12345"}'

# Entrar e guardar o token
$r = Invoke-RestMethod "$base/auth/login" -Method Post -ContentType "application/json" `
  -Body '{"email":"maria@exemplo.com","password":"Senha12345"}'
$headers = @{ Authorization = "Bearer $($r.token)" }

# Dados do usuário logado
Invoke-RestMethod "$base/auth/me" -Headers $headers

# Carregar produtos em lote a partir de um arquivo JSON (exige token de administrador)
Invoke-RestMethod "$base/materials/batch" -Method Post -ContentType "application/json" `
  -Headers $headers -InFile ".\materiais.json"
```

O papel de administrador vai dentro do token, e só é concedido a quem tem o e-mail em `Admin:Emails` **no momento do login**. Depois de incluir um e-mail na lista, entre de novo para receber um token novo.

Localmente, no Swagger, faça o login em `POST /api/auth/login`, copie o valor de `token`, clique em **Authorize** e cole o token sem a palavra `Bearer`.

## Modelo de dados

Exemplo de material:

```json
{
  "id": "c1f6d892-3a5e-4c7b-912a-4f51e0672899",
  "name": "Placa de Gesso Drywall ST Standard 12,5 x 1200 x 1800mm",
  "category": "Placa Drywall",
  "price": 42.9,
  "imageUrl": "https://exemplo.com/imagem.png",
  "company": "Espaço Smart Goiânia",
  "brand": "KNAUF",
  "rating": 4.8,
  "reviewCount": 154,
  "stock": 600,
  "isSponsored": false,
  "hasOwnInstallation": false,
  "specs": {
    "unidadeMedida": "chapa",
    "codigoProduto": "9999-102",
    "sistema": "Drywall"
  }
}
```

- `imageUrl` precisa ser um link **direto para a imagem** (terminando em `.jpg`, `.png` ou `.webp`), e não para a página do produto.
- `specs` é livre: o app monta as linhas da tela de comparação a partir das chaves que existirem.

Tabelas do banco: `Materials` e `AppUsers` (com índice único em `Email`).

## Estrutura do projeto

```
API-HABITARIUM/
├── Controllers/
│   ├── AuthController.cs        # cadastro, login e /me
│   └── MaterialController.cs    # catálogo de materiais
├── Data/
│   └── AppDbContext.cs          # contexto do EF Core
├── Dtos/
│   └── AuthDtos.cs              # modelos de entrada e saída da autenticação
├── Migrations/                  # migrações do banco
├── Models/
│   ├── AppUser.cs
│   └── Material.cs
├── Services/
│   └── TokenService.cs          # geração do token JWT (inclui o papel admin)
├── Properties/launchSettings.json
├── Dockerfile                   # imagem usada na publicação
├── .dockerignore
├── appsettings.json
└── Program.cs                   # configuração dos serviços e do pipeline
```

## Publicação na nuvem

A API roda no **Render**, a partir do `Dockerfile` do repositório, e o banco está no **Neon**.

**Banco (Neon).** Crie um projeto PostgreSQL e use a conexão **direta** (sem *connection pooling*), no formato do Npgsql:

```
Host=<host>;Database=<database>;Username=<usuario>;Password=<senha>;SSL Mode=Require
```

**API (Render).** Crie um *Web Service* a partir do repositório, com runtime **Docker** e plano gratuito, e defina as variáveis de ambiente:

| Variável | Valor |
|---|---|
| `ConnectionStrings__DefaultConnection` | connection string do Neon |
| `Jwt__Key` | chave aleatória de 48 bytes |
| `Admin__Emails` | e-mail(s) de administrador |

Use `/api/materials` como *Health Check Path*. O `Dockerfile` lê a porta da variável `PORT` que o Render informa. As migrações rodam sozinhas na inicialização, e o catálogo pode ser carregado com `POST /api/materials/batch` usando um token de administrador.

## Segurança

- Senhas são guardadas apenas como hash, gerado pelo `PasswordHasher` do ASP.NET Core. A senha em si nunca é armazenada.
- Cadastro e login respondem com a mesma mensagem para e-mail inexistente e senha errada, para não revelar quais e-mails existem.
- A chave do JWT, a connection string e a lista de administradores ficam no *user-secrets* (desenvolvimento) ou em variáveis de ambiente (produção). Nunca as coloque no `appsettings.json` nem no Git.
- Qualquer pessoa pode criar uma conta, mas só os e-mails de `Admin:Emails` recebem o papel `admin` e conseguem alterar o catálogo.
- O Swagger só é habilitado no ambiente de desenvolvimento, então não existe na API publicada.
- A API publicada é servida em HTTPS pelo Render.

## Integração com o app Android

O app consome esta API com Retrofit e OkHttp. A URL base fica em `data/network/RetrofitInstance.kt`, no repositório do app, e aponta para a API publicada. Para desenvolver contra uma API local, veja o README do [app](https://github.com/Arthur-Cardoso0/Projeto-Mobile).

## Próximos passos

- Guardar os orçamentos de cada usuário no servidor.
- Paginação e filtros no `GET /api/materials`.
- Testes automatizados para a autenticação.

## Autor

Desenvolvido por **Arthur Cardoso das Dores**.