namespace GestionCommerciale.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.Seller;

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}

public enum Role
{
    Admin,
    Seller
}