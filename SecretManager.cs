using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AIAssistant;

public class SecretItem
{
    public string Domain { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    public string HiddenPassword => new string('*', Password.Length);
}

public static class SecretManager
{
    private static readonly string SecretFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secrets.enc");
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AIAssistantSecretEntropy");

    public static List<SecretItem> LoadSecrets()
    {
        if (!File.Exists(SecretFilePath)) return new List<SecretItem>();

        try
        {
            byte[] encryptedBytes = File.ReadAllBytes(SecretFilePath);
            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(decryptedBytes);
            return JsonSerializer.Deserialize<List<SecretItem>>(json) ?? new List<SecretItem>();
        }
        catch
        {
            return new List<SecretItem>();
        }
    }

    public static void SaveSecrets(List<SecretItem> secrets)
    {
        try
        {
            string json = JsonSerializer.Serialize(secrets);
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(json);
            byte[] encryptedBytes = ProtectedData.Protect(plaintextBytes, Entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(SecretFilePath, encryptedBytes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save secrets: {ex.Message}");
        }
    }
}
