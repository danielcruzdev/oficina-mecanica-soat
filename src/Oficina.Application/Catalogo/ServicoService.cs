using Oficina.Application.Abstracoes;
using Oficina.Application.Comum;
using Oficina.Domain.Catalogo;
using Oficina.Domain.Comum;

namespace Oficina.Application.Catalogo;

public sealed record ServicoRequest(string Nome, string? Descricao, decimal PrecoMaoDeObra, int TempoEstimadoMinutos);

public sealed record ServicoResponse(Guid Id, string Nome, string? Descricao, decimal PrecoMaoDeObra,
    int TempoEstimadoMinutos, bool Ativo)
{
    public static ServicoResponse De(Servico s) =>
        new(s.Id, s.Nome, s.Descricao, s.PrecoMaoDeObra.Valor, s.TempoEstimadoMinutos, s.Ativo);
}

public sealed class ServicoService(IServicoRepository servicos, IUnitOfWork unitOfWork)
{
    public async Task<ServicoResponse> CriarAsync(ServicoRequest request, CancellationToken ct = default)
    {
        var servico = new Servico(request.Nome, request.Descricao, Dinheiro.Criar(request.PrecoMaoDeObra),
            request.TempoEstimadoMinutos);
        servicos.Adicionar(servico);
        await unitOfWork.SalvarAsync(ct);
        return ServicoResponse.De(servico);
    }

    public async Task<ServicoResponse> ObterAsync(Guid id, CancellationToken ct = default) =>
        ServicoResponse.De(await Carregar(id, ct));

    public async Task<IReadOnlyList<ServicoResponse>> ListarAsync(int? pagina, int? tamanhoPagina,
        CancellationToken ct = default)
    {
        var (p, t) = Paginacao.Normalizar(pagina, tamanhoPagina);
        return (await servicos.ListarAsync(p, t, ct)).Select(ServicoResponse.De).ToList();
    }

    public async Task<ServicoResponse> AtualizarAsync(Guid id, ServicoRequest request, CancellationToken ct = default)
    {
        var servico = await Carregar(id, ct);
        servico.Atualizar(request.Nome, request.Descricao, Dinheiro.Criar(request.PrecoMaoDeObra),
            request.TempoEstimadoMinutos);
        await unitOfWork.SalvarAsync(ct);
        return ServicoResponse.De(servico);
    }

    /// <summary>Remoção lógica: OS antigas continuam referenciando o serviço.</summary>
    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var servico = await Carregar(id, ct);
        servico.Inativar();
        await unitOfWork.SalvarAsync(ct);
    }

    private async Task<Servico> Carregar(Guid id, CancellationToken ct) =>
        await servicos.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Serviço não encontrado.");
}
