namespace Oficina.Infrastructure.Seguranca;

/// <summary>Nomes das claims do JWT emitido pela API.</summary>
public static class Claims
{
    public const string UsuarioId = "sub";
    public const string Papel = "role";
    public const string ClienteId = "cliente_id";
}
