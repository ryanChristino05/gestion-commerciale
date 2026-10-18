# PROGRESS.md — Système de Gestion Commerciale
Mis à jour manuellement par Ryan à chaque session.

---

## Travaux terminés ✅

### Conception
- [x] Cahier des charges rédigé (5 pages, docx)
- [x] Modèle de données défini : 6 tables (Users, Products, Clients, Sales, SaleItems, StockMovements)
- [x] Diagramme MLD généré et compris
- [x] Stack technique validée : ASP.NET Core (.NET 10) + EF Core + Npgsql + JWT + BCrypt + WPF

### Initialisation du projet
- [x] Dossier créé : `D:\Csharp\GestionCommerciale\`
- [x] Projet API créé : `dotnet new webapi -n GestionCommerciale.Api`
- [x] Projet Tests créé : `dotnet new xunit -n GestionCommerciale.Tests`
- [x] Solution créée : `dotnet new sln -n GestionCommerciale`
- [x] Les deux projets ajoutés à la solution
- [x] Dépôt GitHub créé : `github.com/ryanChristino05/gestion-commerciale`

### Packages NuGet installés
- [x] GestionCommerciale.Api :
  - Npgsql.EntityFrameworkCore.PostgreSQL
  - Microsoft.EntityFrameworkCore.Design
  - Microsoft.AspNetCore.Authentication.JwtBearer
  - BCrypt.Net-Next
  - System.IdentityModel.Tokens.Jwt
- [x] GestionCommerciale.Tests :
  - Microsoft.EntityFrameworkCore.InMemory
  - Microsoft.AspNetCore.Mvc.Testing
  - Référence vers GestionCommerciale.Api

### Modèles (Models/)
- [x] `User.cs` — Id, Username, PasswordHash, Role (enum Admin/Seller), ICollection<Sale>
- [x] `Product.cs` — Id, Name, Reference, UnitPrice, Unit, StockQuantity, AlertThreshold, IsArchived
- [x] `StockMovement.cs` — Id, ProductId, Type (enum In/Out), Quantity, Reason, Date, SaleId?
- [x] `Client.cs` — Id, Name, Phone, Email, Address, IsArchived
- [x] `Sale.cs` — Id, Number, Date, ClientId, SellerId, TotalAmount, Status (enum), CancelReason?
- [x] `SaleItem.cs` — Id, SaleId, ProductId, Quantity, UnitPrice, Subtotal

### Data & Configuration
- [x] `AppDbContext.cs` créé avec les 6 DbSet
- [x] `OnModelCreating` configuré :
  - Enums stockés en string
  - Précision décimaux (decimal(18,2))
  - Index unique sur Sale.Number, Product.Reference, User.Username
  - Relations explicites (Sale → User, Sale → Client) avec DeleteBehavior.Restrict
- [x] `Program.cs` configuré : EF Core + JWT + UseRouting + UseAuthentication + UseAuthorization
- [x] `appsettings.json` configuré (PostgreSQL local + JWT)
- [x] Migration `InitialCreate` créée et appliquée sur PostgreSQL local
- [x] Base de données `GestionCommercialeDB` créée et opérationnelle

### Contrôleurs API
- [x] `AuthController.cs` — Register (avec rôle) + Login (retourne token + role)
  - Testé Postman : Register Admin ✅, Login ✅ (token JWT + role reçus)
- [x] `ProductsController.cs` — GET all (paginé + recherche) + GET by id + POST + PUT + DELETE (archive) + GET movements
  - Testé Postman : POST produit ✅, GET liste ✅ (isLowStock calculé)
- [x] `ClientsController.cs` — GET all (recherche) + GET by id + POST + PUT + DELETE (archive)
  - Testé Postman : en cours

---

## En cours 🔄

- [ ] Tester ClientsController dans Postman (POST + GET)

---

## Prochaines étapes (dans l'ordre)

### API — Contrôleurs restants
1. `StockController.cs` — POST /stock/in (entrée de stock manuelle, Admin uniquement)
2. `SalesController.cs` — le cœur du projet :
   - GET /sales (filtrée par rôle : Admin voit tout, Seller voit les siennes)
   - POST /sales (transaction atomique : créer la vente + décrémenter le stock + créer les StockMovements)
   - POST /sales/{id}/cancel (annuler une vente + remettre le stock, Admin uniquement)

### API — Sécurité & qualité
3. Ajouter `.gitignore` (exclure appsettings.json, .env, bin/, obj/)
4. Premier push GitHub

### Tests xUnit
5. Tests sur la logique de vente dans `GestionCommerciale.Tests` :
   - Créer une vente → stock décrémenté ✅
   - Créer une vente → stock insuffisant → 400 Bad Request, aucune modification en base ✅
   - Annuler une vente → stock remis ✅
   - Vendeur ne peut pas annuler → 403 ✅

### Docker & Déploiement
6. Dockerfile multi-stage (même structure que MonApi)
7. `.env` pour les secrets
8. Rebuild image + test conteneur local
9. Push GitHub
10. Déploiement Render (Web Service Docker)
11. Base Neon : créer le projet, récupérer la connection string
12. Appliquer les migrations sur Neon (`dotnet ef database update` pointant vers Neon)
13. Variables d'environnement sur Render
14. Tester tous les endpoints sur l'URL publique

### Client WPF
15. Créer le projet WPF dans la solution : `dotnet new wpf -n GestionCommerciale.WPF`
16. Ajouter HttpClient + Newtonsoft.Json
17. Fenêtre Login (appel POST /auth/login, stockage token en mémoire)
18. Navigation principale (menu latéral selon le rôle)
19. Page Produits : liste, recherche, créer, modifier
20. Page Stock : entrée de stock
21. Page Clients : liste, recherche, créer, modifier
22. Page Ventes : liste, créer une vente (sélection client + lignes produit), annuler
23. Alerte visuelle produits en stock bas

### Finitions
24. README complet sur GitHub (stack, lien API déployée, captures)
25. Mettre à jour le CV avec le lien du projet

---

## Dernières erreurs rencontrées

| Erreur | Cause | Solution |
|---|---|---|
| GET /products retournait liste vide | POST pas encore testé avec le token | Ajouter le header Authorization: Bearer dans Postman |

---

## Décisions techniques prises

- Enums stockés en string en base (lisibles sans jointure)
- Archive au lieu de suppression physique (IsArchived = true)
- UnitPrice stocké dans SaleItem au moment de la vente (prix figé)
- Le vendeur voit uniquement ses propres ventes
- La création d'une vente et la décrémentation du stock sont dans une seule transaction EF Core
- Pas de Swagger (tests via Postman uniquement)
