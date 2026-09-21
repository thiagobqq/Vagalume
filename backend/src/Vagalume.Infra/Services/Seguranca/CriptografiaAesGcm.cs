using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Vagalume.Domain.Abstractions;

namespace Vagalume.Infra.Services.Seguranca;

/// <summary>
/// Cifra segredos (API keys de IA) com AES-256-GCM. A chave de 256 bits é
/// derivada (SHA-256) do segredo <c>Seguranca:ChaveCripto</c>. O formato do
/// valor cifrado é <c>nonce(12) || tag(16) || ciphertext</c> em Base64.
/// Trocar o segredo invalida os valores já gravados (usuário reconfigura).
/// </summary>
public class CriptografiaAesGcm : ICriptografiaService
{
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private readonly byte[] _chave;

    public CriptografiaAesGcm(IConfiguration configuration)
    {
        var segredo = configuration["Seguranca:ChaveCripto"]
            ?? Environment.GetEnvironmentVariable("Seguranca__ChaveCripto")
            ?? "vagalume-dev-chave-cripto-nao-use-em-producao";

        _chave = SHA256.HashData(Encoding.UTF8.GetBytes(segredo));
    }

    public string Proteger(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return texto;

        var plaintext = Encoding.UTF8.GetBytes(texto);
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[TagBytes];

        using var aes = new AesGcm(_chave, TagBytes);
        aes.Encrypt(nonce, plaintext, cipher, tag);

        var resultado = new byte[NonceBytes + TagBytes + cipher.Length];
        Buffer.BlockCopy(nonce, 0, resultado, 0, NonceBytes);
        Buffer.BlockCopy(tag, 0, resultado, NonceBytes, TagBytes);
        Buffer.BlockCopy(cipher, 0, resultado, NonceBytes + TagBytes, cipher.Length);

        return Convert.ToBase64String(resultado);
    }

    public string? Revelar(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
            return null;

        try
        {
            var dados = Convert.FromBase64String(texto);
            if (dados.Length < NonceBytes + TagBytes)
                return null;

            var nonce = dados.AsSpan(0, NonceBytes);
            var tag = dados.AsSpan(NonceBytes, TagBytes);
            var cipher = dados.AsSpan(NonceBytes + TagBytes);
            var plaintext = new byte[cipher.Length];

            using var aes = new AesGcm(_chave, TagBytes);
            aes.Decrypt(nonce, cipher, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch
        {
            // segredo trocado/corrompido: trata como ausente
            return null;
        }
    }
}
