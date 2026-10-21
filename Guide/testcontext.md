# AI_CONTEXT.md — Système de Gestion Commerciale
(mis à jour le 20/10/2026)

## Développeur
- Ryan, 3e année d'école d'ingénieur (Madagascar), cherche un stage en développement
- Stack : C# / ASP.NET Core, Windows + Visual Studio + WSL2 pour Git
- A déjà fait MonApi (Render + Neon), même stack

## Projet
Projet portfolio : produits, stock, clients, ventes (transaction atomique).
API ASP.NET Core (.NET 10) + client bureau WPF.
- API : https://gestion-commerciale-23xb.onrender.com (Render Free, s'endort après 15 min, 30-60 s au réveil)
- Dépôt : github.com/ryanChristino05/gestion-commerciale (branche `master`)
- Local : D:\Csharp\GestionCommerciale\ (Api, Tests, WPF, .sln)
- BDD locale PostgreSQL, prod Neon, auth JWT HS256 (8h) + BCrypt
- Tests xUnit 4/4, Docker OK

## État
| Composant | Statut |
|---|---|
| API, tests, Docker, déploiement | ✅ |
| WPF Login, Dashboard, AddProductWindow | ✅ |
| WPF ProductsPage | ✅ colonnes manuelles (prix "N0 Ar", état stock via convertisseurs) |
| WPF StockPage | 🔄 code écrit, build en échec |
| WPF ClientsPage, SalesPage | ❌ vides |
| Faille /auth/register | ⚠️ à corriger |
| README GitHub, CV | ❌ |

## Bug de build en cours
Erreurs : CS0111 / CS0102 / CS0101 sur `StockPage` (membres « déjà définis »), CS8646 sur `SalesPage.xaml.cs`.
Déjà vérifié :
- un seul `StockPage.xaml.cs` sur le disque, `x:Class` correct
- `.csproj` propre (SDK style, aucun `Compile Include`)
- `StockMovementDto` défini une seule fois
- `StockPage.g.cs` normal, `obj` supprimé sans effet
Hypothèse : le fichier est compilé deux fois (Directory.Build.props, `*_wpftmp.csproj` résiduel, autre).
À lancer :
  dotnet msbuild .\GestionCommerciale.WPF.csproj -getItem:Compile | Select-String "StockPage|SalesPage"
  Get-ChildItem ..\ -Recurse -Include "Directory.Build.*" -ErrorAction SilentlyContinue
  Get-ChildItem . -Filter "*wpftmp*" -Recurse
À vérifier aussi : contenu de `SalesPage.xaml.cs` (et `ClientsPage`), qui peuvent contenir du code dupliqué.

## Prochaines étapes
1. Régler le bug de build
2. ClientsPage (liste + recherche + créer)
3. SalesPage (liste + créer vente avec lignes + annuler)
4. Corriger /auth/register : toujours créer un `Seller`, ajouter `POST /auth/create-user` en `[Authorize(Roles="Admin")]`, redéployer
5. README GitHub + mise à jour CV

## Points techniques
- `System.Text.Json` dans ApiService, `PropertyNameCaseInsensitive = true`
- Stock initial dans `POST /products` : `initialStock` (pas `stockQuantity`)
- Rôles : Admin (stock in, créer/modifier/archiver produit, annuler vente), Seller (ses ventes)
- `StockPage` charge les mouvements produit par produit (`/products/{id}/movements`) ; un endpoint global `/stock/movements` serait mieux plus tard
- Palette WPF : #2D2D2D, #333333, #555555, #F5F5F5, #EEEEEE, pas de couleurs vives
- Convertisseurs : `BoolToStockConverter`, `BoolToColorConverter` (Converters/StockConverters.cs)