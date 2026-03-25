using Microsoft.EntityFrameworkCore;
using SecretVault.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add these two
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