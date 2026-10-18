using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Catalogo;
using Oficina.Domain.Clientes;
using Oficina.Domain.Estoque;
using Oficina.Domain.Identidade;
using Oficina.Domain.Veiculos;

namespace Oficina.Infrastructure.Persistencia.Repositorios;

internal static class Paginacao
{
    public static IQueryable<T> Pagina<T>(this IQueryable<T> query, int pagina, int tamanhoPagina) =>
        query.Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina);
}

internal sealed class ClienteRepository(OficinaDbContext db) : IClienteRepository
{
    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Cliente?> ObterPorDocumentoAsync(Documento documento, CancellationToken ct) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Documento == documento, ct);

    public async Task<IReadOnlyList<Cliente>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken ct) =>
        await db.Clientes.Where(c => c.Ativo).OrderBy(c => c.Nome).Pagina(pagina, tamanhoPagina).ToListAsync(ct);

    public void Adicionar(Cliente cliente) => db.Clientes.Add(cliente);
}

internal sealed class VeiculoRepository(OficinaDbContext db) : IVeiculoRepository
{
    public Task<Veiculo?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Veiculos.FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<Veiculo?> ObterPorPlacaAsync(Placa placa, CancellationToken ct) =>
        db.Veiculos.FirstOrDefaultAsync(v => v.Placa == placa, ct);

    public async Task<IReadOnlyList<Veiculo>> ListarAsync(Guid? clienteId, int pagina, int tamanhoPagina,
        CancellationToken ct) =>
        await db.Veiculos
            .Where(v => clienteId == null || v.ClienteId == clienteId)
            .OrderBy(v => v.Placa)
            .Pagina(pagina, tamanhoPagina)
            .ToListAsync(ct);

    public void Adicionar(Veiculo veiculo) => db.Veiculos.Add(veiculo);

    public void Remover(Veiculo veiculo) => db.Veiculos.Remove(veiculo);
}

internal sealed class ServicoRepository(OficinaDbContext db) : IServicoRepository
{
    public Task<Servico?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Servicos.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Servico>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken ct) =>
        await db.Servicos.Where(s => s.Ativo).OrderBy(s => s.Nome).Pagina(pagina, tamanhoPagina).ToListAsync(ct);

    public void Adicionar(Servico servico) => db.Servicos.Add(servico);
}

internal sealed class PecaRepository(OficinaDbContext db) : IPecaRepository
{
    public Task<Peca?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Pecas.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Peca>> ObterPorIdsAsync(IEnumerable<Guid> ids, CancellationToken ct) =>
        await db.Pecas.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    public async Task<IReadOnlyList<Peca>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken ct) =>
        await db.Pecas.Where(p => p.Ativo).OrderBy(p => p.Nome).Pagina(pagina, tamanhoPagina).ToListAsync(ct);

    public void Adicionar(Peca peca) => db.Pecas.Add(peca);
}

internal sealed class UsuarioRepository(OficinaDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Login == login.Trim().ToLower(), ct);

    public void Adicionar(Usuario usuario) => db.Usuarios.Add(usuario);
}
