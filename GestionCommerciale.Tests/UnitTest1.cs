using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GestionCommerciale.Api.Controllers;
using GestionCommerciale.Api.Data;
using GestionCommerciale.Api.Models;

namespace GestionCommerciale.Tests;

public class SalesTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private SalesController CreateController(AppDbContext db, int userId, string role)
    {
        var controller = new SalesController(db);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
        return controller;
    }

    private async Task<(User user, Product product, Client client)> SeedData(AppDbContext db)
    {
        var user = new User { Username = "vendeur", PasswordHash = "x", Role = Role.Seller };
        var product = new Product
        {
            Name = "Café Arabica",
            Reference = "CAF-001",
            UnitPrice = 5000,
            Unit = "kg",
            StockQuantity = 50,
            AlertThreshold = 10
        };
        var client = new Client { Name = "Jean Rakoto", Phone = "034", Email = "j@j.com", Address = "Tana" };

        db.Users.Add(user);
        db.Products.Add(product);
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        return (user, product, client);
    }

    // Test 1 : Créer une vente → stock décrémenté
    [Fact]
    public async Task Create_VenteValide_StockDecremente()
    {
        var db = CreateDb();
        var (user, product, client) = await SeedData(db);
        var controller = CreateController(db, user.Id, "Seller");

        var dto = new CreateSaleDto
        {
            ClientId = client.Id,
            Items = new List<SaleItemDto>
            {
                new SaleItemDto { ProductId = product.Id, Quantity = 5 }
            }
        };

        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result);

        var updatedProduct = await db.Products.FindAsync(product.Id);
        Assert.Equal(45, updatedProduct!.StockQuantity);
    }

    // Test 2 : Stock insuffisant → 400, stock inchangé
    [Fact]
    public async Task Create_StockInsuffisant_Retourne400()
    {
        var db = CreateDb();
        var (user, product, client) = await SeedData(db);
        var controller = CreateController(db, user.Id, "Seller");

        var dto = new CreateSaleDto
        {
            ClientId = client.Id,
            Items = new List<SaleItemDto>
            {
                new SaleItemDto { ProductId = product.Id, Quantity = 9999 }
            }
        };

        var result = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);

        var updatedProduct = await db.Products.FindAsync(product.Id);
        Assert.Equal(50, updatedProduct!.StockQuantity);
    }

    // Test 3 : Annuler une vente → stock remis
    [Fact]
    public async Task Cancel_VenteValidee_StockRemis()
    {

        var db = CreateDb();
        var (user, product, client) = await SeedData(db);
        var controller = CreateController(db, user.Id, "Admin");

        // Créer une vente d'abord
        var createDto = new CreateSaleDto
        {
            ClientId = client.Id,
            Items = new List<SaleItemDto>
            {
                new SaleItemDto { ProductId = product.Id, Quantity = 10 }
            }
        };
        await controller.Create(createDto);

        // Stock doit être à 40 maintenant
        var sale = await db.Sales.FirstAsync();
        Assert.Equal(40, (await db.Products.FindAsync(product.Id))!.StockQuantity);

        // Annuler la vente
        var cancelResult = await controller.Cancel(sale.Id, new CancelDto { Reason = "Test" });
        Assert.IsType<OkObjectResult>(cancelResult);

        // Stock doit être revenu à 50
        var updatedProduct = await db.Products.FindAsync(product.Id);
        Assert.Equal(50, updatedProduct!.StockQuantity);
    }

    // Test 4 : Vendeur ne peut pas annuler → 403
    [Fact]
    public async Task Cancel_ParVendeur_Retourne403()
    {
        var db = CreateDb();
        var (user, product, client) = await SeedData(db);

        // Créer la vente en tant qu'Admin
        var adminController = CreateController(db, user.Id, "Admin");
        var createDto = new CreateSaleDto
        {
            ClientId = client.Id,
            Items = new List<SaleItemDto>
            {
                new SaleItemDto { ProductId = product.Id, Quantity = 5 }
            }
        };
        await adminController.Create(createDto);
        var sale = await db.Sales.FirstAsync();

        // Tenter d'annuler en tant que Seller
        var sellerController = CreateController(db, user.Id, "Seller");
        var cancelResult = await sellerController.Cancel(sale.Id, new CancelDto { Reason = "Test" });

        Assert.IsType<ForbidResult>(cancelResult);
    }
}