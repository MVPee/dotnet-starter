# Starter

Starter .NET 10 en Clean Architecture : API REST (controllers), EF Core + PostgreSQL, `Result<T>` pour les erreurs métier, tests unitaires / architecture / intégration (Testcontainers), image Docker prête pour Dokploy.

## Architecture

```
   Starter.Api ──────────▶ Starter.Application ◀────────── Starter.Infrastructure
        │                          │                                │
        │                          ▼                                │
        └────────────────▶  Starter.Domain  ◀───────────────────────┘
```

Les flèches pointent vers le cœur. `Api` ne référence `Infrastructure` que pour la composition (DI dans `Program.cs`).

| Projet | Contient | Peut voir |
|---|---|---|
| `Starter.Domain` | Modèles métier (`User`), règles, `Result<T>` / `Error` | rien |
| `Starter.Application` | Use cases, commands, ports (`IUserRepository`, `IUnitOfWork`), filtres paginés | Domain |
| `Starter.Infrastructure` | EF Core (`AppDbContext`, `UserEntity`, configurations, mappers, repositories), migrations — tout est `internal` | Application, Domain |
| `Starter.Api` | Controllers, DTOs, mappers DTO ↔ Command/Model, gestion d'erreurs HTTP | Application, Domain (+ Infrastructure pour la DI) |

Ces règles sont vérifiées par `tests/Starter.ArchitectureTests`.

> **Piège de vocabulaire** : une « Entity » au sens Clean Architecture est notre **modèle Domain** (`User`). `UserEntity` est la **classe de persistance EF Core** et ne sort jamais d'Infrastructure.

## Repositories et Unit of Work

Règle critique, sans exception : chaque repository expose uniquement le CRUD réutilisable de `IRepository<TModel, TFilter>` (`GetByIdAsync`, `GetAllAsync(filter)` paginé, `GetCountAsync(filter)`, `AddBatchAsync`, `UpdateBatchAsync`, `DeleteBatchAsync`). Les implémentations héritent de `Repository<TModel, TEntity, TFilter>` et ne fournissent que `ApplyFilter`, `ToModel` et `ToEntity`.

Les repositories ne font que préparer les changements ; le use case appelle `IUnitOfWork.SaveChangesAsync` une seule fois à la fin (une transaction : tout passe ou rien ne passe). Seuls les use cases écrivent : l'Api n'injecte jamais `IUnitOfWork` ni un repository.

Ces règles sont vérifiées par des tests (et donc par la CI) :

- `RepositoryRulesTests` : forme des repositories (héritage de `Repository<,,>`, `sealed`, seules surcharges autorisées, interfaces `I<Concept>Repository` vides, `AppDbContext` injecté uniquement dans les repositories et `UnitOfWork`).
- `UnitOfWorkRulesTests` : l'Api ne dépend ni d'`IUnitOfWork` ni d'un repository ; dans Application seuls les `*UseCase` dépendent d'`IUnitOfWork` ; un use case contient au plus un appel à `SaveChangesAsync` ; dans Infrastructure seul `UnitOfWork` utilise les membres EF Core qui écrivent (`SaveChanges*`, `ExecuteUpdate/Delete`, `ExecuteSql*`, `BeginTransaction`, `GetDbConnection`), vérifié sur l'IL compilé, délégués compris.
- `UniqueConstraintMappingTests` (intégration) : chaque index unique et clé alternative du modèle EF a son entrée dans `UniqueConstraintErrors`.

## Ajouter un endpoint

1. **Domain** — modèle / règles / erreurs (`<Concept>Errors`) dans `src/Starter.Domain/<Concept>/`.
2. **Application** — use case concret (`<Action>UseCase` + `<Action>Command`) et, pour un nouveau concept, `<Concept>Filter` + `I<Concept>Repository` vide. Enregistrer le use case dans `ApplicationServiceRegistration`.
3. **Infrastructure** — `<Concept>Entity`, `<Concept>EntityConfiguration`, `<Concept>EntityMapper` (Entity ↔ Model), repository héritant de `Repository<...>`, index uniques dans `UniqueConstraintErrors`, enregistrement DI, puis la migration (voir ci-dessous).
4. **Api** — DTOs, `<Concept>DtoMapper` (DTO ↔ Command/Model), action de controller qui convertit le `Result` en réponse HTTP.
5. **Tests** — unitaires (Domain + use case avec un faux repository) et intégration (endpoint contre Postgres réel).

## Développement

Prérequis : Docker. Le SDK .NET 10 (`global.json`) est nécessaire seulement pour lancer les commandes en local.

```bash
docker compose up            # Postgres + API en hot reload sur http://localhost:5080
                             # Scalar : http://localhost:5080/scalar — health : /health
docker compose down          # garde les volumes (base + cache NuGet)
docker compose down -v       # repart de zéro
```

En Development, l'API applique les migrations au démarrage (`Database:ApplyMigrationsOnStartup=true`).

```bash
dotnet tool restore
dotnet build
dotnet format                                  # corrige le style (.editorconfig)
dotnet test                                    # tous les tests ; l'intégration a besoin de Docker
dotnet test tests/Starter.UnitTests

dotnet ef migrations add <Name> \
  --project src/Starter.Infrastructure \
  --startup-project src/Starter.Infrastructure \
  --output-dir Persistence/Migrations
```

`dotnet ef` utilise `DesignTimeDbContextFactory` qui lit `ConnectionStrings__Database` (par défaut : Postgres local du compose).

## Déploiement (Dokploy)

Un seul `Dockerfile` pour staging et production ; seules les variables d'environnement changent.

| Application Dokploy | Build type | Docker target | Rôle |
|---|---|---|---|
| API | Dockerfile | `runtime` (défaut) | Sert l'API sur le port `8080` |
| Migrations | Dockerfile | `migrations` | Job ponctuel avant chaque déploiement de l'API ; applique les migrations puis s'arrête. Lit `ConnectionStrings__Database` depuis l'environnement (jamais en argument, donc invisible dans `ps`) et échoue avec un message clair si elle manque |

Variables d'environnement :

| Variable | API | Migrations |
|---|---|---|
| `ConnectionStrings__Database` | ✔ | ✔ |
| `ASPNETCORE_ENVIRONMENT` | `Staging` / `Production` | — |
| `Database__ApplyMigrationsOnStartup` | `false` (défaut hors Development) | — |
| `AllowedHosts` | **obligatoire** : domaines publics séparés par `;` (ex. `api.example.com`). `*` ou vide fait échouer le démarrage hors Development | — |
| `ForwardedHeaders__TrustedNetworks__0` | **obligatoire** : sous-réseau de `dokploy-network` (ex. `10.0.1.0/24`). Absent ou vide fait échouer le démarrage hors Development | — |
| `RateLimiting__Read__PermitLimit` / `RateLimiting__Write__PermitLimit` | optionnel (défauts : 100 et 10 requêtes par minute et par IP) | — |

- Port du conteneur : `8080` (Traefik termine le TLS devant).
- Si l'API plante au démarrage (configuration invalide, ou base injoignable quand les migrations au démarrage sont activées), le conteneur s'arrête avec le code `1` et l'erreur dans les logs, au lieu de rester bloqué : .NET tourne en PID 1 dans le conteneur et ne s'arrêterait pas tout seul. Un plantage après le démarrage peut mettre jusqu'à 30 s à arrêter le conteneur (délai d'arrêt de l'hôte).
- Health check : `GET /health`. L'image runtime est *chiseled* (pas de shell ni de `curl`), donc pas de `HEALTHCHECK` Docker : configurer la sonde HTTP côté Dokploy/Traefik.
- `/health` vérifie aussi la connexion à Postgres : si la base tombe, il renvoie `503`. Si la sonde redémarre le conteneur à chaque échec, une panne de base provoque des redémarrages en boucle de l'API ; préférer une sonde qui retire le conteneur du trafic sans le redémarrer.
- La sonde doit envoyer un `Host` présent dans `AllowedHosts`, sinon elle reçoit `400`. Si elle appelle le conteneur directement (par IP ou `localhost`), ajouter cette valeur à `AllowedHosts`.
- OpenAPI + Scalar sont exposés hors Production uniquement (donc aussi sur un staging public).
- **IP des clients** : l'API ne lit `X-Forwarded-For` que si la requête vient de `ForwardedHeaders:TrustedNetworks` (Traefik sur `dokploy-network`). Hors Development, l'app refuse de démarrer si cette liste est vide. Trouver le sous-réseau : `docker network inspect dokploy-network --format '{{range .IPAM.Config}}{{.Subnet}}{{end}}'`.
- **Vérifier après le premier déploiement** que l'IP réelle arrive bien : faire une requête depuis un téléphone en 4G et comparer le `from <ip>` des logs de requête avec l'IP publique du téléphone (IPv4 et IPv6). Si toutes les requêtes affichent la même IP interne, le rate limiting bloquerait tout le monde ensemble.
- **Rate limiting** : par IP (IPv6 regroupé par réseau /64, IPv4 « mappée » ramenée à l'IPv4), fenêtre glissante d'une minute. Les valeurs sont validées au démarrage : `Window` s'écrit `hh:mm:ss` (un nombre seul serait lu comme des jours et fait échouer le démarrage). Chaque action déclare sa politique : `read` (100/min) sur les GET, `write` (10/min) sur les écritures, compteurs séparés ; un test d'architecture échoue si une action en oublie une. `/health` n'est jamais limité. Réponse : `429` ProblemDetails avec `code: "rate_limit.exceeded"` et `Retry-After`. Les compteurs sont en mémoire, donc par instance.
- Les logs de requête contiennent l'IP du client (donnée personnelle au sens du RGPD) : limiter la durée de conservation des logs.
- **Pas d'authentification** pour l'instant (choix assumé du starter) : tous les endpoints sont publics, y compris la lecture des emails.
- Builder les images localement :

```bash
docker build --target runtime -t starter-api .
docker build --target migrations -t starter-migrations .
docker run --rm -e ConnectionStrings__Database="Host=...;Database=...;Username=...;Password=..." starter-migrations
```

## CI

Déclenchée sur chaque pull request et sur les push vers `main`. Le setup .NET commun (SDK de `global.json`, cache NuGet, restore) est dans `.github/actions/setup-dotnet`.

| Workflow | Vérifie | Filtré par chemin |
|---|---|---|
| `lint.yml` | `dotnet format --verify-no-changes` | non |
| `test.yml` | unit + architecture, et intégration (Testcontainers) en parallèle | non |
| `migrations.yml` | aucune migration oubliée (`dotnet ef migrations has-pending-model-changes`) | `src/` (hors Api), props, `global.json`, outils, action de setup |
| `docker.yml` | build des images `runtime` et `migrations` (sans push) | `src/`, Dockerfile, props, `.editorconfig` |
| `security.yml` | paquets NuGet vulnérables, directs et transitifs : simple avertissement pour Low/Moderate, échec pour toute sévérité plus haute (ou inconnue) et si l'audit n'a pas pu tourner (source NuGet injoignable). Tourne aussi chaque lundi pour attraper les CVE publiées après un merge ; GitHub n'envoie l'email d'échec du run planifié qu'à la personne qui a modifié le cron en dernier | non |

Checks à rendre obligatoires dans la protection de branche : `Lint`, `Test` et `Security`. Les workflows filtrés par chemin ne tournent pas sur toutes les PR ; obligatoires, ils bloqueraient celles qui ne les déclenchent pas.

Dependabot (`.github/dependabot.yml`) ouvre chaque semaine des PR groupées pour NuGet, les GitHub Actions et les images Docker. L'image SDK est exclue : elle se met à jour à la main, en même temps que `global.json`.

Vérifier les vulnérabilités en local : `.github/scripts/check-vulnerable-packages.sh`.
