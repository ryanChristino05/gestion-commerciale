using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Api.Data;
using GestionCommerciale.Api.Models;

namespace GestionCommerciale.Api.Controllers;

[ApiController]
[Route("clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClientsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /clients?search=dupont
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null)
    {
        var query = _db.Clients.Where(c => !c.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                c.Name.ToLower().Contains(search.ToLower()));

        var clients = await query
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Phone,
                c.Email,
                c.Address
            })
            .ToListAsync();

        return Ok(clients);
    }

    // GET /clients/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var client = await _db.Clients
            .Where(c => c.Id == id && !c.IsArchived)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Phone,
                c.Email,
                c.Address
            })
            .FirstOrDefaultAsync();

        if (client == null) return NotFound();
        return Ok(client);
    }

    // POST /clients
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientDto dto)
    {
        var client = new Client
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = client.Id }, new { client.Id });
    }

    // PUT /clients/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ClientDto dto)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null || client.IsArchived) return NotFound();

        client.Name = dto.Name;
        client.Phone = dto.Phone;
        client.Email = dto.Email;
        client.Address = dto.Address;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /clients/5 (archive uniquement)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Archive(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null || client.IsArchived) return NotFound();

        client.IsArchived = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class ClientDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}