namespace Oficina.Domain.Identidade;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken ct = default);
    void Adicionar(Usuario usuario);
}
