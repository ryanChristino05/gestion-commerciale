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

**Statut actuel :**
- API complète ✅, déployée et validée en production ✅
- Docker ✅
- Client WPF en cours : Login ✅, Dashboard ✅, ProductsPage (partielle) ✅, AddProductWindow ✅
- Pages restantes : StockPage, ClientsPage, SalesPage

**URL publique API : https://gestion-commerciale-23xb.onrender.com**

---

## Emplacement local

```
D:\Csharp\GestionCommerciale\
├── GestionCommerciale.Api\        ← API ASP.NET Core (.NET 10) — COMPLÈTE
├── GestionCommerciale.Tests\      ← Tests xUnit — 4/4 passants
├── GestionCommerciale.WPF\        ← Client WPF en cours
│   ├── Services\
│   │   └── ApiService.cs          ← Centralise tous les appels HTTP
│   ├── Views\
│   │   ├── ProductsPage.xaml/.cs  ← En cours (AutoGenerateColumns, à améliorer)
│   │   ├── StockPage.xaml/.cs     ← Vide, à remplir
│   │   ├── ClientsPage.xaml/.cs   ← Vide, à remplir
│   │   └── SalesPage.xaml/.cs     ← Vide, à remplir
│   ├── Converters\
│   │   └── StockConverters.cs     ← BoolToStockConverter + BoolToColorConverter
│   ├── MainWindow.xaml/.cs        ← Fenêtre Login ✅
│   ├── DashboardWindow.xaml/.cs   ← Navigation principale ✅
│   └── AddProductWindow.xaml/.cs  ← Dialogue création produit ✅
├── .env
├── .gitignore
└── GestionCommerciale.sln
```

Dépôt GitHub : `github.com/ryanChristino05/gestion-commerciale` (branche master)

---

## Stack technique

| Couche | Technologie | Statut |
|---|---|---|
| Backend | ASP.NET Core Web API (.NET 10) | ✅ Complet |
| ORM | Entity Framework Core + Npgsql | ✅ |
| Base locale | PostgreSQL — GestionCommercialeDB | ✅ |
| Base prod | Neon — neondb (ep-young-block-b4b1dg7m-pooler.c-6.us-east-2.aws.neon.tech) | ✅ Migrations appliquées |
| Auth | JWT HS256 (8h) + BCrypt | ✅ |
| Tests | xUnit + EF InMemory | ✅ 4/4 |
| Docker | Image gestion-commerciale:latest | ✅ Testée en local |
| Hébergement | Render (Free, Docker) | ✅ En ligne |
| Client | WPF (.NET 10) | 🔄 En cours |

---

## ⚠️ Faille de sécurité connue — à corriger

`POST /auth/register` est public et accepte `"role": "Admin"` dans le body.
N'importe qui peut donc créer un compte Admin.

**Correction à faire dans AuthController.cs :**
1. Forcer `/auth/register` (public) à toujours créer un compte `Role.Seller` — ignorer le champ `role` du DTO
2. Ajouter un endpoint `POST /auth/create-user` protégé par `[Authorize(Roles = "Admin")]` pour créer admins/vendeurs
3. Redéployer sur Render

Le compte admin déjà créé en prod reste valide après la correction.

---

## Modèles (Models/) — COMPLETS

```csharp
public class User {
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.Seller;
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
public enum Role { Admin, Seller }

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

public class Client {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsArchived { get; set; } = false;
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}

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

## Endpoints API — COMPLETS

| Méthode | Route | Rôle | Description |
|---|---|---|---|
| POST | /auth/register | Public | Crée un compte (⚠️ accepte "Admin" — faille à corriger) |
| POST | /auth/login | Public | Retourne { token, role } |
| GET | /products | Tous | Liste paginée + recherche + isLowStock |
| GET | /products/{id} | Tous | Détail produit |
| POST | /products | Admin | Créer produit + StockMovement initial (champ : initialStock) |
| PUT | /products/{id} | Admin | Modifier produit |
| DELETE | /products/{id} | Admin | Archiver produit |
| GET | /products/{id}/movements | Tous | Historique mouvements |
| GET | /clients | Tous | Liste + recherche |
| GET | /clients/{id} | Tous | Détail client |
| POST | /clients | Tous | Créer client |
| PUT | /clients/{id} | Tous | Modifier client |
| DELETE | /clients/{id} | Admin | Archiver client |
| POST | /stock/in | Admin | Entrée de stock manuelle |
| GET | /sales | Tous | Admin=tout, Seller=les siennes |
| GET | /sales/{id} | Tous | Détail vente + lignes |
| POST | /sales | Tous | Créer vente (transaction atomique) |
| POST | /sales/{id}/cancel | Admin | Annuler vente + remettre stock |

---

## ApiService.cs (Services/ApiService.cs) — VERSION ACTUELLE

```csharp
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GestionCommerciale.WPF.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string? Token { get; private set; }
    public static string? Role { get; private set; }

    public ApiService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri("https://gestion-commerciale-23xb.onrender.com")
        };
    }

    private void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Authorization =
            Token != null ? new AuthenticationHeaderValue("Bearer", Token) : null;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var body = JsonSerializer.Serialize(new { username, password });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/auth/login", content);
        if (!response.IsSuccessStatusCode) return false;
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<LoginResponse>(json, _json);
        if (result is null) return false;
        Token = result.Token;
        Role = result.Role;
        SetAuthHeader();
        return true;
    }

    public void Logout()
    {
        Token = null;
        Role = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _json);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
    {
        var body = JsonSerializer.Serialize(data);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        return await _http.PostAsync(url, content);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string url, T data)
    {
        var body = JsonSerializer.Serialize(data);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        return await _http.PutAsync(url, content);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
        => await _http.DeleteAsync(url);

    // PRODUITS
    public async Task<ProductsResult?> GetProductsAsync(string search = "", int page = 1)
        => await GetAsync<ProductsResult>($"/products?page={page}&pageSize=20&search={search}");

    public async Task<bool> CreateProductAsync(object dto)
    {
        var response = await PostAsync("/products", dto);
        return response.IsSuccessStatusCode;
    }

    // CLIENTS
    public async Task<List<ClientDto>?> GetClientsAsync(string search = "")
        => await GetAsync<List<ClientDto>>($"/clients?search={search}");

    public async Task<bool> CreateClientAsync(object dto)
    {
        var response = await PostAsync("/clients", dto);
        return response.IsSuccessStatusCode;
    }

    // STOCK
    public async Task<bool> StockInAsync(int productId, int quantity, string reason)
    {
        var response = await PostAsync("/stock/in", new { productId, quantity, reason });
        return response.IsSuccessStatusCode;
    }

    // VENTES
    public async Task<List<SaleDto>?> GetSalesAsync()
        => await GetAsync<List<SaleDto>>("/sales");

    public async Task<(bool success, string message)> CreateSaleAsync(object dto)
    {
        var response = await PostAsync("/sales", dto);
        var body = await response.Content.ReadAsStringAsync();
        return (response.IsSuccessStatusCode, body);
    }

    public async Task<bool> CancelSaleAsync(int saleId, string reason)
    {
        var response = await PostAsync($"/sales/{saleId}/cancel", new { reason });
        return response.IsSuccessStatusCode;
    }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class ProductsResult
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<ProductDto> Products { get; set; } = new();
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int AlertThreshold { get; set; }
    public bool IsLowStock { get; set; }
}

public class ClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class SaleDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string Seller { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}
```

---

## État actuel de ProductsPage

- `AutoGenerateColumns="True"` activé (solution rapide de ChatGPT)
- À améliorer : remplacer par des colonnes définies manuellement avec formatage prix et alerte stock
- Convertisseurs créés : `BoolToStockConverter` et `BoolToColorConverter` dans `Converters/StockConverters.cs`
- Déclarés dans `Page.Resources` de `ProductsPage.xaml`

**Bug corrigé :** le champ JSON envoyé à l'API pour le stock initial était `stockQuantity` au lieu de `initialStock` — corrigé dans `AddProductWindow.xaml.cs`

---

## Prochaine étape exacte

1. Améliorer `ProductsPage` : remplacer `AutoGenerateColumns="True"` par colonnes manuelles avec formatage
2. Créer `StockPage` (formulaire entrée de stock : sélection produit + quantité + motif)
3. Créer `ClientsPage` (liste + recherche + créer)
4. Créer `SalesPage` (liste + créer vente avec lignes + annuler)
5. Corriger la faille de sécurité `/auth/register` et redéployer
6. README GitHub + mise à jour CV

---

## Notes importantes pour la prochaine IA

- Le projet WPF utilise `System.Text.Json` (pas Newtonsoft) dans ApiService
- Les DTOs de l'API utilisent la casse camelCase côté JSON, mais `PropertyNameCaseInsensitive = true` est activé dans ApiService donc la désérialisation est tolérante
- Le champ pour le stock initial dans le DTO de création produit s'appelle `initialStock` (pas `stockQuantity`) — erreur déjà rencontrée et corrigée
- La branche Git s'appelle `master` (pas `main`)
- Le plan Free Render s'endort après 15 min — la première requête peut prendre 30-60s
- Palette de couleurs WPF choisie par Ryan : sobre, gris foncé (#2D2D2D, #333333, #555555) et blanc/gris clair (#F5F5F5, #EEEEEE) — pas de couleurs vives
