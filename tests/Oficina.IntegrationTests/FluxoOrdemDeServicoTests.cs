using System.Net;
using System.Net.Http.Json;
using Oficina.Application.OrdensDeServico;
using Oficina.Application.Relatorios;
using Oficina.Domain.OrdensDeServico;
using Oficina.IntegrationTests.Infra;

namespace Oficina.IntegrationTests;

[Collection(ApiCollection.Nome)]
public class FluxoOrdemDeServicoTests(OficinaApiFactory factory)
{
    private readonly ApiTeste _api = new(factory);

    [Fact]
    public async Task Fluxo_completo_da_abertura_a_entrega()
    {
        var peca = await _api.NovaPeca(saldo: 10, preco: 35m);
        var (cliente, senha) = await _api.NovoCliente();

        var enviada = await _api.OsAguardandoAprovacao(cliente, peca.Id, quantidade: 3);
        Assert.Equal(StatusOS.AguardandoAprovacao, enviada.Status);
        Assert.Equal(120m + 3 * 35m, enviada.Orcamento.Total);
        Assert.Equal(SituacaoOrcamento.Enviado, enviada.Orcamento.Situacao);
        Assert.Equal(10, (await _api.Peca(peca.Id)).Saldo);

        // Cliente acompanha e aprova pela API
        var area = await _api.Cliente(cliente.Documento, senha);
        var minhas = await ApiTeste.Ler<List<OrdemDeServicoResumoResponse>>(await area.GetAsync("/api/v1/me/ordens-servico"));
        Assert.Contains(minhas, o => o.Id == enviada.Id);

        var aprovada = await ApiTeste.Ler<OrdemDeServicoResponse>(
            await area.PostAsync($"/api/v1/me/ordens-servico/{enviada.Id}/orcamento/aprovacao", null));
        Assert.Equal(StatusOS.EmExecucao, aprovada.Status);
        Assert.Equal(7, (await _api.Peca(peca.Id)).Saldo);

        var finalizada = await ApiTeste.Ler<OrdemDeServicoResponse>(await (await _api.Mecanico())
            .PostAsync($"/api/v1/ordens-servico/{enviada.Id}/execucao/finalizacao", null));
        Assert.Equal(StatusOS.Finalizada, finalizada.Status);
        Assert.Equal(ResultadoOS.Concluida, finalizada.Resultado);

        var entregue = await ApiTeste.Ler<OrdemDeServicoResponse>(await (await _api.Atendente())
            .PostAsync($"/api/v1/ordens-servico/{enviada.Id}/entrega", null));
        Assert.Equal(StatusOS.Entregue, entregue.Status);
        Assert.Equal(
            [StatusOS.Recebida, StatusOS.EmDiagnostico, StatusOS.AguardandoAprovacao, StatusOS.EmExecucao,
                StatusOS.Finalizada, StatusOS.Entregue],
            entregue.Historico.Select(h => h.Para));

        var detalheDoCliente = await ApiTeste.Ler<OrdemDeServicoResponse>(
            await area.GetAsync($"/api/v1/me/ordens-servico/{enviada.Id}"));
        Assert.Equal(StatusOS.Entregue, detalheDoCliente.Status);

        var tempo = await ApiTeste.Ler<TempoMedioExecucaoResponse>(
            await (await _api.Admin()).GetAsync("/api/v1/relatorios/tempo-medio-execucao"));
        Assert.True(tempo.Quantidade >= 1);
        Assert.NotNull(tempo.MediaMinutos);
    }

    [Fact]
    public async Task Recusa_finaliza_a_os_sem_baixar_estoque()
    {
        var peca = await _api.NovaPeca(saldo: 5);
        var (cliente, senha) = await _api.NovoCliente();
        var os = await _api.OsAguardandoAprovacao(cliente, peca.Id, quantidade: 2);

        var recusada = await ApiTeste.Ler<OrdemDeServicoResponse>(await (await _api.Cliente(cliente.Documento, senha))
            .PostAsJsonAsync($"/api/v1/me/ordens-servico/{os.Id}/orcamento/recusa", new RecusarOrcamentoRequest("Caro")));

        Assert.Equal(StatusOS.Finalizada, recusada.Status);
        Assert.Equal(ResultadoOS.OrcamentoRecusado, recusada.Resultado);
        Assert.Equal("Caro", recusada.Orcamento.MotivoRecusa);
        Assert.Equal(5, (await _api.Peca(peca.Id)).Saldo);
    }

    [Fact]
    public async Task Falta_de_estoque_na_aprovacao_retorna_422_e_nada_e_gravado()
    {
        var peca = await _api.NovaPeca(saldo: 2);
        var (cliente, senha) = await _api.NovoCliente();
        // A inclusão valida disponibilidade sem reservar: as duas OS conseguem incluir 2 unidades.
        var primeira = await _api.OsAguardandoAprovacao(cliente, peca.Id, quantidade: 2);
        var segunda = await _api.OsAguardandoAprovacao(cliente, peca.Id, quantidade: 2);
        var area = await _api.Cliente(cliente.Documento, senha);

        var ok = await area.PostAsync($"/api/v1/me/ordens-servico/{primeira.Id}/orcamento/aprovacao", null);
        var semEstoque = await area.PostAsync($"/api/v1/me/ordens-servico/{segunda.Id}/orcamento/aprovacao", null);

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, semEstoque.StatusCode);
        var segundaDepois = await ApiTeste.Ler<OrdemDeServicoResponse>(await area.GetAsync($"/api/v1/me/ordens-servico/{segunda.Id}"));
        Assert.Equal(StatusOS.AguardandoAprovacao, segundaDepois.Status);
        Assert.Equal(0, (await _api.Peca(peca.Id)).Saldo);
    }

    [Fact]
    public async Task Aprovacoes_simultaneas_nao_deixam_estoque_negativo()
    {
        var peca = await _api.NovaPeca(saldo: 3);
        var (cliente, senha) = await _api.NovoCliente();
        var primeira = await _api.OsAguardandoAprovacao(cliente, peca.Id, quantidade: 2);
        var segunda = await _api.OsAguardandoAprovacao(cliente, peca.Id, quantidade: 2);
        var area = await _api.Cliente(cliente.Documento, senha);

        var respostas = await Task.WhenAll(
            area.PostAsync($"/api/v1/me/ordens-servico/{primeira.Id}/orcamento/aprovacao", null),
            area.PostAsync($"/api/v1/me/ordens-servico/{segunda.Id}/orcamento/aprovacao", null));

        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respostas, r => r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity);
        Assert.Equal(1, (await _api.Peca(peca.Id)).Saldo);
    }

    [Fact]
    public async Task Cliente_nao_ve_nem_aprova_os_de_outro_cliente_404()
    {
        var peca = await _api.NovaPeca(saldo: 5);
        var (dono, _) = await _api.NovoCliente();
        var (outro, senhaOutro) = await _api.NovoCliente();
        var os = await _api.OsAguardandoAprovacao(dono, peca.Id, quantidade: 1);
        var intruso = await _api.Cliente(outro.Documento, senhaOutro);

        Assert.Equal(HttpStatusCode.NotFound, (await intruso.GetAsync($"/api/v1/me/ordens-servico/{os.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await intruso.PostAsync($"/api/v1/me/ordens-servico/{os.Id}/orcamento/aprovacao", null)).StatusCode);
        Assert.DoesNotContain(
            await ApiTeste.Ler<List<OrdemDeServicoResumoResponse>>(await intruso.GetAsync("/api/v1/me/ordens-servico")),
            o => o.Id == os.Id);
        Assert.Equal(5, (await _api.Peca(peca.Id)).Saldo);
    }

    [Fact]
    public async Task Regras_de_transicao_e_orcamento()
    {
        var (cliente, _) = await _api.NovoCliente();
        var atendente = await _api.Atendente();
        var os = await ApiTeste.Ler<OrdemDeServicoResponse>(await atendente.PostAsJsonAsync("/api/v1/ordens-servico",
            new AbrirOrdemDeServicoRequest(cliente.Documento,
                new VeiculoDaOrdemRequest(ApiTeste.GerarPlaca(), "VW", "Gol", 2020), "Não liga")), HttpStatusCode.Created);

        // Enviar sem diagnóstico: transição inválida
        Assert.Equal(HttpStatusCode.Conflict,
            (await atendente.PostAsync($"/api/v1/ordens-servico/{os.Id}/orcamento/envio", null)).StatusCode);

        await (await _api.Mecanico()).PostAsync($"/api/v1/ordens-servico/{os.Id}/diagnostico", null);

        // Enviar sem itens: regra de negócio
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await atendente.PostAsync($"/api/v1/ordens-servico/{os.Id}/orcamento/envio", null)).StatusCode);
        // Entregar sem finalizar
        Assert.Equal(HttpStatusCode.Conflict,
            (await atendente.PostAsync($"/api/v1/ordens-servico/{os.Id}/entrega", null)).StatusCode);
    }

    [Fact]
    public async Task Incluir_e_remover_itens_recalcula_orcamento()
    {
        var peca = await _api.NovaPeca(saldo: 5, preco: 10m);
        var (cliente, _) = await _api.NovoCliente();
        var mecanico = await _api.Mecanico();
        var os = await ApiTeste.Ler<OrdemDeServicoResponse>(await (await _api.Atendente()).PostAsJsonAsync(
            "/api/v1/ordens-servico", new AbrirOrdemDeServicoRequest(cliente.Documento,
                new VeiculoDaOrdemRequest(ApiTeste.GerarPlaca(), "VW", "Gol", 2020), "Revisão")), HttpStatusCode.Created);

        var comPeca = await ApiTeste.Ler<OrcamentoResponse>(await mecanico.PostAsJsonAsync(
            $"/api/v1/ordens-servico/{os.Id}/pecas", new AdicionarPecaRequest(peca.Id, 2)));
        var semSaldo = await mecanico.PostAsJsonAsync($"/api/v1/ordens-servico/{os.Id}/pecas",
            new AdicionarPecaRequest(peca.Id, 10));
        var semPeca = await ApiTeste.Ler<OrcamentoResponse>(
            await mecanico.DeleteAsync($"/api/v1/ordens-servico/{os.Id}/pecas/{comPeca.Pecas[0].Id}"));
        var consultado = await ApiTeste.Ler<OrcamentoResponse>(await mecanico.GetAsync($"/api/v1/ordens-servico/{os.Id}/orcamento"));

        Assert.Equal(20m, comPeca.Total);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, semSaldo.StatusCode);
        Assert.Equal(0m, semPeca.Total);
        Assert.Equal(2, consultado.Versao);
    }

    [Fact]
    public async Task Listagem_prioriza_os_em_andamento_e_filtra_por_placa()
    {
        var (cliente, _) = await _api.NovoCliente();
        var atendente = await _api.Atendente();
        var placa = ApiTeste.GerarPlaca();
        var recebida = await ApiTeste.Ler<OrdemDeServicoResponse>(await atendente.PostAsJsonAsync("/api/v1/ordens-servico",
            new AbrirOrdemDeServicoRequest(cliente.Documento, new VeiculoDaOrdemRequest(placa, "VW", "Gol", 2020), "A")),
            HttpStatusCode.Created);
        var peca = await _api.NovaPeca(saldo: 5);
        var aguardando = await _api.OsAguardandoAprovacao(cliente, peca.Id, quantidade: 1);

        var lista = await ApiTeste.Ler<List<OrdemDeServicoResumoResponse>>(
            await atendente.GetAsync($"/api/v1/ordens-servico?documento={cliente.Documento}"));
        var porPlaca = await ApiTeste.Ler<List<OrdemDeServicoResumoResponse>>(
            await atendente.GetAsync($"/api/v1/ordens-servico?placa={placa}"));
        var detalhe = await ApiTeste.Ler<OrdemDeServicoResponse>(await atendente.GetAsync($"/api/v1/ordens-servico/{recebida.Id}"));

        Assert.Equal([aguardando.Id, recebida.Id], lista.Select(o => o.Id));
        Assert.Equal(recebida.Id, Assert.Single(porPlaca).Id);
        Assert.Equal(placa, detalhe.Veiculo.Placa);

        // Veículo com OS não pode ser removido
        Assert.Equal(HttpStatusCode.Conflict,
            (await atendente.DeleteAsync($"/api/v1/veiculos/{recebida.Veiculo.Id}")).StatusCode);
    }
}
