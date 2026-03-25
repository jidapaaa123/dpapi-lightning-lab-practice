using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using SecretVault.Data;
using SecretVault.Models;

namespace SecretVault.Controllers;

[ApiController]
[Route("[controller]")]
public class VaultController : ControllerBase
{
    private readonly IDataProtector _protector;
    private readonly VaultDbContext _db;

    public VaultController(IDataProtectionProvider provider, VaultDbContext db)
    {
        _protector = provider.CreateProtector("SecretVault.v1");
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Store([FromBody] string value)
    {
        var entry = new VaultEntry
        {
            EncryptedValue = _protector.Protect(value)
        };

        _db.VaultEntries.Add(entry);
        await _db.SaveChangesAsync();

        return Ok(new { entry.Id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Retrieve(int id)
    {
        var entry = await _db.VaultEntries.FindAsync(id);
        if (entry is null) return NotFound();

        var plaintext = _protector.Unprotect(entry.EncryptedValue);
        return Ok(plaintext);
    }

    [HttpGet("{id}/raw")]
    public async Task<IActionResult> Raw(int id)
    {
        var entry = await _db.VaultEntries.FindAsync(id);
        if (entry is null) return NotFound();

        return Ok(entry.EncryptedValue);
    }
}