using Oficina.Application.Abstracoes;
using Oficina.Application.Comum;
using Oficina.Domain.Comum;
using Oficina.Domain.Estoque;

namespace Oficina.Application.Estoque;

public sealed record CriarPecaRequest(string Sku, string Nome, TipoItem Tipo, string Unidade, decimal Preco,
    int EstoqueMinimo, int SaldoInicial);

public sealed record AtualizarPecaRequest(string Nome, TipoItem Tipo, string Unidade, decimal Preco, int EstoqueMinimo);

public sealed record EntradaEstoqueRequest(int Quantidade);

public sealed record PecaResponse(Guid Id, string Sku, string Nome, TipoItem Tipo, string Unidade, decimal Preco,
    int Saldo, int EstoqueMinimo, bool Ativo)
{
    public static PecaResponse De(Peca p) =>
        new(p.Id, p.Sku, p.Nome, p.Tipo, p.Unidade, p.Preco.Valor, p.Saldo, p.EstoqueMinimo, p.Ativo);
}

public sealed class PecaService(IPecaRepository pecas, IUnitOfWork unitOfWork, TimeProvider relogio)
{
    public async Task<PecaResponse> CriarAsync(CriarPecaRequest request, CancellationToken ct = default)
    {
        var peca = new Peca(request.Sku, request.Nome, request.Tipo, request.Unidade, Dinheiro.Criar(request.Preco),
            request.EstoqueMinimo, request.SaldoInicial, relogio.GetUtcNow());
        pecas.Adicionar(peca);
        await unitOfWork.SalvarAsync(ct);
        return PecaResponse.De(peca);
    }

    public async Task<PecaResponse> ObterAsync(Guid id, CancellationToken ct = default) =>
        PecaResponse.De(await Carregar(id, ct));

    public async Task<IReadOnlyList<PecaResponse>> ListarAsync(int? pagina, int? tamanhoPagina,
        CancellationToken ct = default)
    {
        var (p, t) = Paginacao.Normalizar(pagina, tamanhoPagina);
        return (await pecas.ListarAsync(p, t, ct)).Select(PecaResponse.De).ToList();
    }

    public async Task<PecaResponse> AtualizarAsync(Guid id, AtualizarPecaRequest request, CancellationToken ct = default)
    {
        var peca = await Carregar(id, ct);
        peca.Atualizar(request.Nome, request.Tipo, request.Unidade, Dinheiro.Criar(request.Preco), request.EstoqueMinimo);
        await unitOfWork.SalvarAsync(ct);
        return PecaResponse.De(peca);
    }

    /// <summary>Remoção lógica: o histórico de movimentações e as OS antigas são preservados.</summary>
    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var peca = await Carregar(id, ct);
        peca.Inativar();
        await unitOfWork.SalvarAsync(ct);
    }

    public async Task<PecaResponse> RegistrarEntradaAsync(Guid id, EntradaEstoqueRequest request,
        CancellationToken ct = default)
    {
        var peca = await Carregar(id, ct);
        peca.RegistrarEntrada(request.Quantidade, relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(ct);
        return PecaResponse.De(peca);
    }

    private async Task<Peca> Carregar(Guid id, CancellationToken ct) =>
        await pecas.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Peça não encontrada.");
}
