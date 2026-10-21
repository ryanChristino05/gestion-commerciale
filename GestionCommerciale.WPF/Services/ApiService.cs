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
        SetAuthHeader();
    }

    private void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Authorization =
            Token != null ? new AuthenticationHeaderValue("Bearer", Token) : null;
    }

    // AUTH
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

    // HELPERS GÉNÉRIQUES
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

        if (response.IsSuccessStatusCode)
            return true;

        string error = await response.Content.ReadAsStringAsync();

        throw new HttpRequestException(
            $"Erreur HTTP {(int)response.StatusCode} " +
            $"({response.StatusCode}) : {error}");
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

// DTOs internes
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
    public int Status { get; set; }
    public string Client { get; set; } = string.Empty;
    public string Seller { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}