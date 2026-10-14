using Oficina.Application.Abstracoes;
using Oficina.Application.Clientes;
using Oficina.Application.Comum;
using Oficina.Application.Veiculos;
using Oficina.Domain.Catalogo;
using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;
using Oficina.Domain.Estoque;
using Oficina.Domain.OrdensDeServico;
using Oficina.Domain.Veiculos;

namespace Oficina.Application.OrdensDeServico;

/// <summary>Casos de uso da OS. Cada método público é uma transação (um único SalvarAsync).</summary>
public sealed class OrdemDeServicoService(
    IOrdemDeServicoRepository ordens,
    IClienteRepository clientes,
    IVeiculoRepository veiculos,
    IServicoRepository servicos,
    IPecaRepository pecas,
    IUnitOfWork unitOfWork,
    TimeProvider relogio)
{
    public async Task<OrdemDeServicoResponse> AbrirAsync(AbrirOrdemDeServicoRequest request, Guid usuarioId,
        CancellationToken ct = default)
    {
        var cliente = await clientes.ObterPorDocumentoAsync(Documento.Criar(request.DocumentoCliente), ct)
            ?? throw new NaoEncontradoException("Cliente não encontrado para o CPF/CNPJ informado.");
        if (!cliente.Ativo)
            throw new DomainException("Cliente inativo.");

        var placa = Placa.Criar(request.Veiculo?.Placa);
        var veiculo = await veiculos.ObterPorPlacaAsync(placa, ct);
        if (veiculo is null)
        {
            veiculo = new Veiculo(cliente.Id, placa, request.Veiculo!.Marca ?? "", request.Veiculo.Modelo ?? "",
                request.Veiculo.Ano ?? 0);
            veiculos.Adicionar(veiculo);
        }
        else if (veiculo.ClienteId != cliente.Id)
        {
            throw new DomainException("O veículo informado pertence a outro cliente.");
        }

        var agora = relogio.GetUtcNow();
        var numero = $"OS-{agora.Year}-{await ordens.ProximoSequencialAsync(ct):D6}";
        var os = OrdemDeServico.Abrir(numero, cliente.Id, veiculo.Id, request.DescricaoProblema, usuarioId, agora);
        ordens.Adicionar(os);

        await unitOfWork.SalvarAsync(ct);
        return Montar(os, cliente, veiculo);
    }

    public async Task<OrdemDeServicoResponse> ObterAsync(Guid id, CancellationToken ct = default) =>
        await MontarAsync(await Carregar(id, ct), ct);

    /// <summary>Sem status, lista só as OS em andamento, priorizadas (EmExecucao > AguardandoAprovacao > ...).</summary>
    public async Task<IReadOnlyList<OrdemDeServicoResumoResponse>> ListarAsync(ListarOrdensDeServicoRequest request,
        CancellationToken ct = default)
    {
        var (pagina, tamanho) = Paginacao.Normalizar(request.Pagina, request.TamanhoPagina);
        var filtro = new FiltroOrdensDeServico(
            request.Status,
            string.IsNullOrWhiteSpace(request.Documento) ? null : Documento.Criar(request.Documento),
            string.IsNullOrWhiteSpace(request.Placa) ? null : Placa.Criar(request.Placa),
            request.De,
            request.Ate,
            pagina,
            tamanho);

        return (await ordens.ListarAsync(filtro, ct)).Select(OrdemDeServicoResumoResponse.De).ToList();
    }

    public async Task<IReadOnlyList<OrdemDeServicoResumoResponse>> ListarDoClienteAsync(Guid clienteId,
        CancellationToken ct = default) =>
        (await ordens.ListarPorClienteAsync(clienteId, ct)).Select(OrdemDeServicoResumoResponse.De).ToList();

    public async Task<OrdemDeServicoResponse> ObterDoClienteAsync(Guid id, Guid clienteId,
        CancellationToken ct = default) =>
        await MontarAsync(await CarregarDoCliente(id, clienteId, ct), ct);

    public async Task<OrdemDeServicoResponse> IniciarDiagnosticoAsync(Guid id, Guid usuarioId,
        CancellationToken ct = default)
    {
        var os = await Carregar(id, ct);
        os.IniciarDiagnostico(usuarioId, relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(ct);
        return await MontarAsync(os, ct);
    }

    public async Task<OrcamentoResponse> AdicionarServicoAsync(Guid id, AdicionarServicoRequest request,
        CancellationToken ct = default)
    {
        var os = await Carregar(id, ct);
        var servico = await servicos.ObterPorIdAsync(request.ServicoId, ct)
            ?? throw new NaoEncontradoException("Serviço não encontrado.");
        if (!servico.Ativo)
            throw new DomainException("Serviço inativo não pode ser incluído na OS.");

        os.AdicionarServico(servico.Id, servico.Nome, servico.PrecoMaoDeObra);
        await unitOfWork.SalvarAsync(ct);
        return OrcamentoResponse.De(os);
    }

    public async Task<OrcamentoResponse> RemoverServicoAsync(Guid id, Guid itemId, CancellationToken ct = default)
    {
        var os = await Carregar(id, ct);
        if (!os.RemoverServico(itemId))
            throw new NaoEncontradoException("Item de serviço não encontrado na OS.");

        await unitOfWork.SalvarAsync(ct);
        return OrcamentoResponse.De(os);
    }

    /// <summary>Valida a disponibilidade no momento da inclusão, sem reservar. A baixa acontece na aprovação.</summary>
    public async Task<OrcamentoResponse> AdicionarPecaAsync(Guid id, AdicionarPecaRequest request,
        CancellationToken ct = default)
    {
        var os = await Carregar(id, ct);
        var peca = await pecas.ObterPorIdAsync(request.PecaId, ct)
            ?? throw new NaoEncontradoException("Peça não encontrada.");
        if (!peca.Ativo)
            throw new DomainException("Peça inativa não pode ser incluída na OS.");

        var jaIncluida = os.QuantidadePorPeca().GetValueOrDefault(peca.Id);
        if (!peca.TemDisponivel(jaIncluida + request.Quantidade))
            throw new DomainException($"Estoque insuficiente para \"{peca.Nome}\": saldo {peca.Saldo}.");

        os.AdicionarPeca(peca.Id, peca.Nome, request.Quantidade, peca.Preco);
        await unitOfWork.SalvarAsync(ct);
        return OrcamentoResponse.De(os);
    }

    public async Task<OrcamentoResponse> RemoverPecaAsync(Guid id, Guid itemId, CancellationToken ct = default)
    {
        var os = await Carregar(id, ct);
        if (!os.RemoverPeca(itemId))
            throw new NaoEncontradoException("Item de peça não encontrado na OS.");

        await unitOfWork.SalvarAsync(ct);
        return OrcamentoResponse.De(os);
    }

    public async Task<OrcamentoResponse> ObterOrcamentoAsync(Guid id, CancellationToken ct = default) =>
        OrcamentoResponse.De(await Carregar(id, ct));

    /// <summary>O "envio" é a disponibilização do orçamento na API do cliente (evento OrcamentoEnviado no log).</summary>
    public async Task<OrdemDeServicoResponse> EnviarOrcamentoAsync(Guid id, Guid usuarioId,
        CancellationToken ct = default)
    {
        var os = await Carregar(id, ct);
        os.EnviarOrcamento(usuarioId, relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(ct);
        return await MontarAsync(os, ct);
    }

    /// <summary>
    /// Aprovação + baixa de estoque na mesma transação: se faltar saldo de qualquer peça,
    /// a exceção acontece antes do SalvarAsync e nada é gravado.
    /// </summary>
    public async Task<OrdemDeServicoResponse> AprovarOrcamentoAsync(Guid id, Guid clienteId, Guid usuarioId,
        CancellationToken ct = default)
    {
        var os = await CarregarDoCliente(id, clienteId, ct);
        var agora = relogio.GetUtcNow();
        os.AprovarOrcamento(usuarioId, agora);

        var quantidades = os.QuantidadePorPeca();
        foreach (var peca in await pecas.ObterPorIdsAsync(quantidades.Keys, ct))
            peca.BaixarPorOS(quantidades[peca.Id], os.Id, agora);

        await unitOfWork.SalvarAsync(ct);
        return await MontarAsync(os, ct);
    }

    public async Task<OrdemDeServicoResponse> RecusarOrcamentoAsync(Guid id, Guid clienteId, Guid usuarioId,
        RecusarOrcamentoRequest request, CancellationToken ct = default)
    {
        var os = await CarregarDoCliente(id, clienteId, ct);
        os.RecusarOrcamento(request.Motivo, usuarioId, relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(ct);
        return await MontarAsync(os, ct);
    }

    public async Task<OrdemDeServicoResponse> FinalizarExecucaoAsync(Guid id, Guid usuarioId,
        CancellationToken ct = default)
    {
        var os = await Carregar(id, ct);
        os.FinalizarExecucao(usuarioId, relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(ct);
        return await MontarAsync(os, ct);
    }

    public async Task<OrdemDeServicoResponse> RegistrarEntregaAsync(Guid id, Guid usuarioId,
        CancellationToken ct = default)
    {
        var os = await Carregar(id, ct);
        os.RegistrarEntrega(usuarioId, relogio.GetUtcNow());
        await unitOfWork.SalvarAsync(ct);
        return await MontarAsync(os, ct);
    }

    private async Task<OrdemDeServico> Carregar(Guid id, CancellationToken ct) =>
        await ordens.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Ordem de serviço não encontrada.");

    /// <summary>OS de outro cliente responde como inexistente (evita BOLA/IDOR).</summary>
    private async Task<OrdemDeServico> CarregarDoCliente(Guid id, Guid clienteId, CancellationToken ct)
    {
        var os = await ordens.ObterPorIdAsync(id, ct);
        return os is not null && os.ClienteId == clienteId
            ? os
            : throw new NaoEncontradoException("Ordem de serviço não encontrada.");
    }

    private async Task<OrdemDeServicoResponse> MontarAsync(OrdemDeServico os, CancellationToken ct)
    {
        var cliente = await clientes.ObterPorIdAsync(os.ClienteId, ct);
        var veiculo = await veiculos.ObterPorIdAsync(os.VeiculoId, ct);
        return Montar(os, cliente!, veiculo!);
    }

    private static OrdemDeServicoResponse Montar(OrdemDeServico os, Cliente cliente, Veiculo veiculo) => new(
        os.Id,
        os.Numero,
        os.Status,
        os.Resultado,
        os.DescricaoProblema,
        ClienteResponse.De(cliente),
        VeiculoResponse.De(veiculo),
        OrcamentoResponse.De(os),
        os.Historico.OrderBy(h => h.OcorridoEm)
            .Select(h => new HistoricoStatusResponse(h.De, h.Para, h.OcorridoEm)).ToList(),
        os.AbertaEm,
        os.InicioExecucao,
        os.FimExecucao,
        os.EntregueEm,
        os.TempoDeExecucao?.TotalMinutes);
}
