using Oficina.Application.Abstracoes;
using Oficina.Application.Comum;
using Oficina.Domain.Clientes;
using Oficina.Domain.OrdensDeServico;
using Oficina.Domain.Veiculos;

namespace Oficina.Application.Veiculos;

public sealed record CriarVeiculoRequest(Guid ClienteId, string Placa, string Marca, string Modelo, int Ano);

public sealed record AtualizarVeiculoRequest(string Placa, string Marca, string Modelo, int Ano);

public sealed record VeiculoResponse(Guid Id, Guid ClienteId, string Placa, string Marca, string Modelo, int Ano)
{
    public static VeiculoResponse De(Veiculo v) => new(v.Id, v.ClienteId, v.Placa.Valor, v.Marca, v.Modelo, v.Ano);
}

public sealed class VeiculoService(
    IVeiculoRepository veiculos,
    IClienteRepository clientes,
    IOrdemDeServicoRepository ordens,
    IUnitOfWork unitOfWork)
{
    public async Task<VeiculoResponse> CriarAsync(CriarVeiculoRequest request, CancellationToken ct = default)
    {
        var placa = Placa.Criar(request.Placa);
        if (await clientes.ObterPorIdAsync(request.ClienteId, ct) is null)
            throw new NaoEncontradoException("Cliente não encontrado.");
        await GarantirPlacaDisponivel(placa, null, ct);

        var veiculo = new Veiculo(request.ClienteId, placa, request.Marca, request.Modelo, request.Ano);
        veiculos.Adicionar(veiculo);
        await unitOfWork.SalvarAsync(ct);
        return VeiculoResponse.De(veiculo);
    }

    public async Task<VeiculoResponse> ObterAsync(Guid id, CancellationToken ct = default) =>
        VeiculoResponse.De(await Carregar(id, ct));

    public async Task<IReadOnlyList<VeiculoResponse>> ListarAsync(Guid? clienteId, int? pagina, int? tamanhoPagina,
        CancellationToken ct = default)
    {
        var (p, t) = Paginacao.Normalizar(pagina, tamanhoPagina);
        return (await veiculos.ListarAsync(clienteId, p, t, ct)).Select(VeiculoResponse.De).ToList();
    }

    public async Task<VeiculoResponse> AtualizarAsync(Guid id, AtualizarVeiculoRequest request,
        CancellationToken ct = default)
    {
        var veiculo = await Carregar(id, ct);
        var placa = Placa.Criar(request.Placa);
        await GarantirPlacaDisponivel(placa, id, ct);

        veiculo.Atualizar(placa, request.Marca, request.Modelo, request.Ano);
        await unitOfWork.SalvarAsync(ct);
        return VeiculoResponse.De(veiculo);
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var veiculo = await Carregar(id, ct);
        if (await ordens.ExisteParaVeiculoAsync(id, ct))
            throw new ConflitoException("Veículo possui ordens de serviço e não pode ser removido.");

        veiculos.Remover(veiculo);
        await unitOfWork.SalvarAsync(ct);
    }

    private async Task GarantirPlacaDisponivel(Placa placa, Guid? veiculoId, CancellationToken ct)
    {
        var existente = await veiculos.ObterPorPlacaAsync(placa, ct);
        if (existente is not null && existente.Id != veiculoId)
            throw new ConflitoException("Já existe um veículo com esta placa.");
    }

    private async Task<Veiculo> Carregar(Guid id, CancellationToken ct) =>
        await veiculos.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Veículo não encontrado.");
}
