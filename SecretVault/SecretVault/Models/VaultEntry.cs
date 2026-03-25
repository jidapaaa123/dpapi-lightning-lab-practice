namespace SecretVault.Models;

public class VaultEntry
{
    public int Id { get; set; }
    public string EncryptedValue { get; set; } = string.Empty;
}