# Système de Gestion Commerciale
![CI](https://github.com/ryanChristino05/gestion-commerciale/actions/workflows/ci.yml/badge.svg)

Application de gestion commerciale complète (produits, stock, clients, ventes) composée d'une **API REST ASP.NET Core** et d'un **client bureau WPF**. Projet réalisé dans le cadre d'un portfolio pour la recherche de stage en développement.

🔗 **API en ligne :** [https://gestion-commerciale-23xb.onrender.com](https://gestion-commerciale-23xb.onrender.com)
> ⚠️ Hébergée sur le plan gratuit de Render : l'API se met en veille après 15 minutes d'inactivité. La première requête peut prendre 30 à 60 secondes.

---

## Fonctionnalités

- **Authentification** sécurisée par JWT (BCrypt pour les mots de passe)
- **Gestion des rôles** : Admin et Vendeur, avec permissions différenciées
- **Gestion des produits** : création, modification, archivage, alerte de stock bas
- **Gestion du stock** : entrées de stock manuelles avec historique des mouvements
- **Gestion des clients** : création, recherche
- **Gestion des ventes** : panier multi-produits, vérification automatique du stock, génération de numéro de vente, annulation avec remise en stock
- **Gestion des utilisateurs** (Admin) : création de comptes Admin ou Vendeur directement depuis le client WPF

---

## Architecture

```
GestionCommerciale/
├── GestionCommerciale.Api/      API ASP.NET Core (.NET 10)
├── GestionCommerciale.Tests/    Tests unitaires xUnit
└── GestionCommerciale.WPF/      Client bureau WPF (.NET 10)
```

| Composant | Technologie |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10) |
| ORM | Entity Framework Core + Npgsql |
| Base de données | PostgreSQL (Neon en production) |
| Authentification | JWT (HS256) + BCrypt |
| Tests | xUnit + EF Core InMemory |
| Conteneurisation | Docker |
| Hébergement | Render |
| Client | WPF (.NET 10) |

---

## Logique métier

### Rôles

L'application distingue deux rôles :

| Action | Admin | Vendeur |
|---|---|---|
| Consulter les produits | ✅ | ✅ |
| Créer / modifier / archiver un produit | ✅ | ❌ |
| Effectuer une entrée de stock | ✅ | ❌ |
| Créer un client | ✅ | ✅ |
| Archiver un client | ✅ | ❌ |
| Créer une vente | ✅ | ✅ |
| Consulter les ventes | Toutes | Les siennes uniquement |
| Annuler une vente | ✅ | ❌ |
| Créer un utilisateur | ✅ | ❌ |

### Stock partagé

Le stock des produits est **partagé entre tous les vendeurs** : il n'existe qu'un seul stock global par produit. Chaque vente décrémente ce stock commun, et chaque vendeur voit l'état du stock mis à jour en temps réel. L'historique des mouvements de stock (entrées et sorties) permet de retracer qui a vendu quoi, grâce à la traçabilité par `SellerId`.

### Transaction atomique sur les ventes

La création d'une vente est **atomique** : vérification du stock disponible pour chaque ligne, décrémentation du stock, création des lignes de vente et du mouvement de stock associé se font en une seule opération. En cas d'échec, rien n'est enregistré.

---

## Sécurité

- Mots de passe hashés avec **BCrypt**
- Authentification par token **JWT** (expiration 8h)
- Endpoints sensibles protégés par rôle (`[Authorize(Roles = "Admin")]`)
- Création de compte Admin réservée aux administrateurs déjà authentifiés (`POST /auth/create-user`) — l'inscription publique (`POST /auth/register`) ne crée que des comptes Vendeur
- Contrôle des rôles appliqué à la fois côté client (WPF) et côté serveur (API), pour une double protection

---

## Principaux endpoints de l'API

| Méthode | Route | Accès | Description |
|---|---|---|---|
| POST | `/auth/register` | Public | Crée un compte Vendeur |
| POST | `/auth/login` | Public | Authentifie et retourne un token JWT |
| POST | `/auth/create-user` | Admin | Crée un compte Admin ou Vendeur |
| GET | `/products` | Authentifié | Liste paginée des produits |
| POST | `/products` | Admin | Crée un produit |
| POST | `/stock/in` | Admin | Entrée de stock manuelle |
| GET | `/clients` | Authentifié | Liste des clients |
| POST | `/clients` | Authentifié | Crée un client |
| GET | `/sales` | Authentifié | Liste des ventes (filtrée selon le rôle) |
| POST | `/sales` | Authentifié | Crée une vente (transaction atomique) |
| POST | `/sales/{id}/cancel` | Admin | Annule une vente et remet le stock à jour |

---

## Lancer le projet en local

### Prérequis
- .NET 10 SDK
- PostgreSQL (local) ou une base Neon
- Visual Studio 2022 (pour le client WPF)

### API

```bash
cd GestionCommerciale.Api
dotnet restore
dotnet ef database update
dotnet run
```

### Tests

```bash
cd GestionCommerciale.Tests
dotnet test
```

### Client WPF

Ouvrir `GestionCommerciale.sln` dans Visual Studio, définir `GestionCommerciale.WPF` comme projet de démarrage, puis lancer.

### Docker (API)

L'API est conteneurisée avec Docker pour garantir un environnement d'exécution identique entre le développement local et la production.

```bash
docker build -t gestion-commerciale -f GestionCommerciale.Api/Dockerfile .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="<connection-string>" \
  -e Jwt__Key="<clé-secrète>" \
  -e Jwt__Issuer="<issuer>" \
  -e Jwt__Audience="<audience>" \
  gestion-commerciale
```

> Les secrets (connection string, clé JWT) ne sont jamais commités dans le dépôt : ils sont fournis via variables d'environnement, aussi bien en local qu'en production.

### Déploiement en production

- **Hébergement :** [Render](https://render.com), via l'image Docker buildée automatiquement à partir du `Dockerfile`
- **Déploiement continu :** chaque push sur la branche `master` déclenche un redéploiement automatique du service Render
- **Base de données :** PostgreSQL managée par [Neon](https://neon.tech), avec les migrations Entity Framework appliquées via `dotnet ef database update`
- **Variables d'environnement** configurées directement dans le dashboard Render (connection string Neon, clé JWT, issuer, audience)
- **Plan utilisé :** Render Free — le service se met en veille après 15 minutes d'inactivité (premier appel plus lent après une période de veille)

---
## CI/CD

Ce projet utilise GitHub Actions pour automatiser le build, les tests et le déploiement :

1. À chaque push sur `master`, l'API est automatiquement compilée et les tests xUnit sont exécutés
2. Si et seulement si tout passe, un déploiement est déclenché automatiquement sur [Render](https://render.com)
3. Base de données PostgreSQL hébergée sur [Neon](https://neon.tech)

```
push → build → tests → déploiement automatique (si tout est vert)
```

## Auteur

**Ryan** — Étudiant en 3e année d'école d'ingénieur à Madagascar, en recherche de stage en développement.
