# AI_CONTEXT.md — Système de Gestion Commerciale
Ce fichier permet à une IA de reprendre le travail sans perdre de contexte.
Mis à jour manuellement par Ryan à chaque session.

---

## Profil du développeur

- Prénom : Ryan
- Étudiant en 3e année d'école d'ingénieur à Madagascar
- Cherche un stage en programmation et développement
- Langage principal : C# / ASP.NET Core
- Travaille sur Windows avec Visual Studio + WSL2 pour Git
- A déjà réalisé MonApi (API déployée sur Render + Neon) — même stack

---

## Contexte du projet

Projet portfolio pour le stage. Système de gestion commerciale :
produits, stock, clients, ventes avec transaction atomique.
Architecture : API ASP.NET Core + client bureau WPF.
On avance vite, on comprend avant de faire.

**Statut actuel : API en cours — Auth ✅, Produits ✅, Clients ✅ (à tester)**

---

## Emplacement local

```
D:\Csharp\GestionCommerciale\
├── GestionCommerciale.Api\        ← API ASP.NET Core (.NET 10)
├── GestionCommerciale.Tests\      ← Tests xUnit
└── GestionCommerciale.sln
```

Dépôt GitHub : `github.com/ryanChristino05/gestion-commerciale` (branche master)

---

## Stack technique

| Couche | Technologie |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10) |
| ORM | Entity Framework Core + Npgsql |
| Base locale | PostgreSQL — base : GestionCommercialeDB |
| Base prod | Neon (PostgreSQL managé, pas encore créé) |
| Auth | JWT HS256 (validité 8h) + BCrypt |
| Client | WPF (.NET 10) — pas encore créé |
| Conteneur | Docker (pas encore fait) |
| Hébergement | Render (pas encore fait) |
| Tests | xUnit + EF InMemory |

---

## Modèles (Models/)

```csharp
// User.cs
public class User {
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.Seller;
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
public enum Role { Admin, Seller }

// Product.cs
public class Product {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int AlertThreshold { get; set; }
    public bool IsArchived { get; set; } = false;
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}

// StockMovement.cs
public class StockMovement {
    public int Id { get; set; }
    public int ProductId { get; set; }
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int? SaleId { get; set; }
    public Product Product { get; set; } = null!;
    public Sale? Sale { get; set; }
}
public enum MovementType { In, Out }

// Client.cs
public class Client {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsArchived { get; set; } = false;
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}

// Sale.cs
public class Sale {
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int ClientId { get; set; }
    public int SellerId { get; set; }
    public decimal TotalAmount { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Validated;
    public string? CancelReason { get; set; }
    public Client Client { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
public enum SaleStatus { Validated, Cancelled }

// SaleItem.cs
public class SaleItem {
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public Sale Sale { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
```

---

## AppDbContext (Data/AppDbContext.cs)

```csharp
using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Api.Models;

namespace GestionCommerciale.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();
        modelBuilder.Entity<Sale>().Property(s => s.Status).HasConversion<string>();
        modelBuilder.Entity<StockMovement>().Property(m => m.Type).HasConversion<string>();
        modelBuilder.Entity<Product>().Property(p => p.UnitPrice).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<SaleItem>().Property(si => si.UnitPrice).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<SaleItem>().Property(si => si.Subtotal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Sale>().HasIndex(s => s.Number).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(p => p.Reference).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Sale>().HasOne(s => s.Seller).WithMany(u => u.Sales)
            .HasForeignKey(s => s.SellerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Sale>().HasOne(s => s.Client).WithMany(c => c.Sales)
            .HasForeignKey(s => s.ClientId).OnDelete(DeleteBehavior.Restrict);
    }
}
```

---

## Program.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GestionCommerciale.Api.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## appsettings.json (local uniquement, ne pas commiter)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=GestionCommercialeDB;Username=postgres;Password=MOT_DE_PASSE_LOCAL;"
  },
  "Jwt": {
    "Key": "GestionCommercialeCléSecrèteTrèsLongueEtSécurisée2026!",
    "Issuer": "GestionCommerciale",
    "Audience": "GestionCommercialeUsers"
  }
}
```

---

## Contrôleurs terminés

### AuthController (Controllers/AuthController.cs)
- POST /auth/register — crée un compte (champ role : "Admin" ou "Seller")
- POST /auth/login — retourne { token, role }
- Claims JWT : NameIdentifier (userId), Name (username), Role
- Token valide 8h

### ProductsController (Controllers/ProductsController.cs)
- GET /products?page=1&pageSize=10&search=xxx — liste paginée, filtrée, isLowStock calculé
- GET /products/{id}
- POST /products — Admin uniquement, crée produit + StockMovement initial si stock > 0
- PUT /products/{id} — Admin uniquement
- DELETE /products/{id} — Admin uniquement (archive, IsArchived = true)
- GET /products/{id}/movements — historique des mouvements

### ClientsController (Controllers/ClientsController.cs)
- GET /clients?search=xxx
- GET /clients/{id}
- POST /clients — tous les rôles
- PUT /clients/{id} — tous les rôles
- DELETE /clients/{id} — Admin uniquement (archive)

---

## Contrôleurs à créer (dans l'ordre)

### 1. StockController
- POST /stock/in — Admin uniquement
- Body : { productId, quantity, reason }
- Incrémente Product.StockQuantity + crée StockMovement (Type = In)

### 2. SalesController — le cœur du projet
- GET /sales — Admin voit tout, Seller voit les siennes (filtrer sur SellerId = userId du token)
- POST /sales — transaction atomique :
  1. Vérifier stock suffisant pour chaque ligne (sinon 400)
  2. Créer la Sale avec numéro auto (format : VNT-YYYYMMDD-XXXX)
  3. Créer les SaleItems
  4. Décrémenter Product.StockQuantity pour chaque ligne
  5. Créer les StockMovements (Type = Out)
  6. Tout dans une seule transaction EF Core (SaveChangesAsync)
- POST /sales/{id}/cancel — Admin uniquement :
  1. Vérifier que la vente est en statut Validated
  2. Remettre le stock (incrémenter Product.StockQuantity)
  3. Créer des StockMovements (Type = In, Reason = "Annulation vente X")
  4. Passer la vente en Cancelled + enregistrer CancelReason

---

## Tests à écrire (GestionCommerciale.Tests)

Logique de vente uniquement (le plus important) :
1. POST /sales avec stock suffisant → 201, stock décrémenté
2. POST /sales avec stock insuffisant → 400, aucune modification en base
3. POST /sales/{id}/cancel → stock remis, statut Cancelled
4. Vendeur tente POST /sales/{id}/cancel → 403

---

## Dockerfile (à créer après les contrôleurs)

Même structure que MonApi — contexte parent GestionCommerciale\ :
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY GestionCommerciale.Api/GestionCommerciale.Api.csproj GestionCommerciale.Api/
COPY GestionCommerciale.Tests/GestionCommerciale.Tests.csproj GestionCommerciale.Tests/
RUN dotnet restore GestionCommerciale.Api/GestionCommerciale.Api.csproj
COPY GestionCommerciale.Api/ GestionCommerciale.Api/
COPY GestionCommerciale.Tests/ GestionCommerciale.Tests/
RUN dotnet publish GestionCommerciale.Api/GestionCommerciale.Api.csproj -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "GestionCommerciale.Api.dll"]
```

Commande de build (depuis D:\Csharp\GestionCommerciale) :
```powershell
docker build -t gestion-commerciale -f GestionCommerciale.Api\Dockerfile .
```

---

## Variables d'environnement Render (à configurer lors du déploiement)

| Key | Value |
|---|---|
| ConnectionStrings__DefaultConnection | Host=...neon.tech;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require |
| Jwt__Key | GestionCommercialeCléSecrèteTrèsLongueEtSécurisée2026! |
| Jwt__Issuer | GestionCommerciale |
| Jwt__Audience | GestionCommercialeUsers |
| ASPNETCORE_URLS | http://+:8080 |

---

## Client WPF (à créer après le déploiement API)

- Projet : `dotnet new wpf -n GestionCommerciale.WPF`
- Pattern MVVM (ViewModels + Commands)
- HttpClient singleton pour appeler l'API déployée
- Token JWT stocké en mémoire (pas sur disque)
- Fenêtres : Login → Main (navigation selon rôle) → Produits / Stock / Clients / Ventes

---

## Prochaine étape exacte

Créer StockController.cs, puis SalesController.cs (la transaction atomique).
