https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/using-data-protection?view=aspnetcore-10.0&source=recommendations  
# Lightning Lab: Encryption with ASP.NET Core Data Protection
Don't (or no need to) clone the repo. It's my completed codebase to prove that it worked on my machine once if anything goes bad  

## What the Lab Creates  
A Web API called **SecretVault** that encrypts data before storing it in a SQLite database and decrypts it on retrieval. The database never sees plaintext, but the server does...
> So this is **NOT** end-to-end encryption. If the server is compromised, the keys go with it. 
> The DB is Sqlite for convenience sake since it's not the focus of the lab


## Endpoints You'll Build
- POST /vault  
- GET /vault/{id}  
- GET /vault/{id}/raw  

---

## Step 1: Scaffold the Project

### In Visual Studio:
1. **File -> New -> Project**
2. Select **ASP.NET Core Web API**, hit Next
3. Name it `SecretVault` (to match with these instr.), hit Next
4. Make sure **"Use controllers"** & **Enable OpenAPI** & **Configure for HTTPS** is checked, hit Create

### Add NuGet Packages:
Right-click the project -> **Manage NuGet Packages**, search and install:
- `Microsoft.AspNetCore.DataProtection`
- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.Design`
- `Swashbuckle.AspNet.Core`

---

## Step 2: Configure Services

Open `Program.cs` and add two lines in the services section, above `var app = builder.Build()`:

```csharp
using SecretVault.Data;

// ...

builder.Services.AddDataProtection();
builder.Services.AddDbContext<VaultDbContext>(options =>
    options.UseSqlite("Data Source=vault.db"));
```

Your `Program.cs` should look like this:

```csharp
using SecretVault.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDataProtection();
builder.Services.AddDbContext<VaultDbContext>(options =>
    options.UseSqlite("Data Source=vault.db"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

> `VaultDbContext` doesn't exist yet btw

---

## Step 3: Build the Data Layer

### Create `Models/VaultEntry.cs`

```csharp
namespace SecretVault.Models;

public class VaultEntry
{
    public int Id { get; set; }
    public string EncryptedValue { get; set; } = string.Empty;
}
```

### Create `Data/VaultDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SecretVault.Models;

namespace SecretVault.Data;

public class VaultDbContext : DbContext
{
    public VaultDbContext(DbContextOptions<VaultDbContext> options) : base(options) { }

    public DbSet<VaultEntry> VaultEntries { get; set; }
}
```

### Run Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Step 4: Build the Controller

Create `Controllers/VaultController.cs` (or replace the WeatherforecastController.cs):

```csharp
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

    // POST /vault
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

    // GET /vault/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Retrieve(int id)
    {
        var entry = await _db.VaultEntries.FindAsync(id);
        if (entry is null) return NotFound();

        var plaintext = _protector.Unprotect(entry.EncryptedValue);
        return Ok(plaintext);
    }

    // GET /vault/{id}/raw
    [HttpGet("{id}/raw")]
    public async Task<IActionResult> Raw(int id)
    {
        var entry = await _db.VaultEntries.FindAsync(id);
        if (entry is null) return NotFound();

        return Ok(entry.EncryptedValue);
    }
}
```

**Things to notice:**
- `CreateProtector("SecretVault.v1")`  
> A purpose string provides isolation between consumers. For example, a protector created with a purpose string of "green" wouldn't be able to unprotect data provided by a protector with a purpose of "purple".
- `Protect()` and `Unprotect()` 
- The `/raw` endpoint  

---

## Step 5: Try it

> If you restarted this server on a completely different machine and pointed it at the same database...

when you call `Unprotect()`, it will throw, because the keys don't travel with the database. 

TL;DR the meat of it is in VaultController.cs, which handles creating a Protector of type IDataProtector. calling that instance with .Protect(text) encrypts the text, and .Unprotect() decrypts the text. You can consider other stuff part of setting up a Controller API, but the DPAPI itself is just those lines (after packages are installed)
