# PROGRESS.md — Système de Gestion Commerciale
# (Version mise à jour — octobre 2026)
# Ce fichier est tenu à jour par Ryan à chaque session de travail.
# La version complète et détaillée a été rédigée avec ChatGPT le 17/10/2026.
# Résumé de l'état actuel ci-dessous pour la prochaine IA.

---

## État global au 17/10/2026

| Composant | Statut |
|---|---|
| API ASP.NET Core | ✅ Complète et déployée |
| Tests xUnit (4/4) | ✅ Passants |
| Docker | ✅ Image buildée et testée |
| Déploiement Render + Neon | ✅ En ligne |
| Client WPF — Login | ✅ Fonctionnel |
| Client WPF — Dashboard | ✅ Navigation fonctionnelle |
| Client WPF — ProductsPage | 🔄 Partielle (AutoGenerateColumns, à améliorer) |
| Client WPF — AddProductWindow | ✅ Fonctionnelle (bug initialStock corrigé) |
| Client WPF — StockPage | ❌ Vide, à créer |
| Client WPF — ClientsPage | ❌ Vide, à créer |
| Client WPF — SalesPage | ❌ Vide, à créer |
| Sécurité /auth/register | ⚠️ Faille connue, correction à faire |
| README GitHub | ❌ À rédiger |
| CV mis à jour | ❌ À faire |

---

## URL publique API
https://gestion-commerciale-23xb.onrender.com

---

## Prochaines étapes dans l'ordre

1. Améliorer ProductsPage (colonnes manuelles, formatage prix, alerte stock)
2. StockPage — formulaire entrée de stock
3. ClientsPage — liste + créer
4. SalesPage — liste + créer vente + annuler
5. Corriger faille /auth/register + redéployer
6. README GitHub + CV

---

## Points techniques importants pour la prochaine IA

- Palette WPF : gris foncé (#2D2D2D, #333333) + blanc/gris clair (#F5F5F5) — PAS de couleurs vives
- ApiService utilise System.Text.Json (pas Newtonsoft)
- DTOs : PropertyNameCaseInsensitive = true dans ApiService
- Champ stock initial dans POST /products : "initialStock" (pas "stockQuantity")
- Branche Git : master (pas main)
- Plan Free Render : s'endort après 15 min, 30-60s au réveil
- Convertisseurs WPF créés : BoolToStockConverter + BoolToColorConverter (Converters/StockConverters.cs)
- Déclarés dans Page.Resources de ProductsPage.xaml

---

## Voir aussi

Le PROGRESS détaillé complet (avec historique des erreurs, décisions techniques
et objectif pédagogique) a été généré par ChatGPT et fourni à Claude le 17/10/2026.
Il est disponible dans les fichiers du projet si besoin.
