using Microsoft.AspNetCore.Identity;
using Oficina.Application.Abstracoes;

namespace Oficina.Infrastructure.Seguranca;

/// <summary>PBKDF2 via PasswordHasher do ASP.NET Core Identity.</summary>
internal sealed class SenhaHasher : ISenhaHasher
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Usuario = new();

    public string Gerar(string senha) => Hasher.HashPassword(Usuario, senha);

    public bool Verificar(string hash, string senha) =>
        Hasher.VerifyHashedPassword(Usuario, hash, senha) != PasswordVerificationResult.Failed;
}
