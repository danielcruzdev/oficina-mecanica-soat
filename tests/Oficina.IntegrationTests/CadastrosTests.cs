using System.Net;
using System.Net.Http.Json;
using Oficina.Application.Clientes;
using Oficina.Application.Estoque;
using Oficina.Application.Veiculos;
using Oficina.IntegrationTests.Infra;

namespace Oficina.IntegrationTests;

[Collection(ApiCollection.Nome)]
public class CadastrosTests(OficinaApiFactory factory)
{
    private readonly ApiTeste _api = new(factory);

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("529.982.247-24")]
    [InlineData("11.222.333/0001-82")]
    public async Task Documento_invalido_retorna_400(string documento)
    {
        var resposta = await (await _api.Atendente()).PostAsJsonAsync("/api/v1/clientes",
            new CriarClienteRequest(documento, "Fulano", null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Documento_duplicado_retorna_409()
    {
        var atendente = await _api.Atendente();
        var request = new CriarClienteRequest(ApiTeste.GerarCpf(), "Fulano", null, null, null);
        await ApiTeste.Ler<ClienteResponse>(await atendente.PostAsJsonAsync("/api/v1/clientes", request), HttpStatusCode.Created);

        var duplicado = await atendente.PostAsJsonAsync("/api/v1/clientes", request);

        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
    }

    [Fact]
    public async Task Crud_de_cliente()
    {
        var atendente = await _api.Atendente();
        var (cliente, _) = await _api.NovoCliente();

        var porDocumento = await ApiTeste.Ler<ClienteResponse>(
            await atendente.GetAsync($"/api/v1/clientes/documento/{cliente.Documento}"));
        var atualizado = await ApiTeste.Ler<ClienteResponse>(await atendente.PutAsJsonAsync($"/api/v1/clientes/{cliente.Id}",
            new AtualizarClienteRequest("Nome Novo", null, "11988887777")));
        var removido = await atendente.DeleteAsync($"/api/v1/clientes/{cliente.Id}");
        var depois = await ApiTeste.Ler<ClienteResponse>(await atendente.GetAsync($"/api/v1/clientes/{cliente.Id}"));

        Assert.Equal(cliente.Id, porDocumento.Id);
        Assert.Equal("Nome Novo", atualizado.Nome);
        Assert.Equal(HttpStatusCode.NoContent, removido.StatusCode);
        Assert.False(depois.Ativo);
    }

    [Theory]
    [InlineData("AB12345")]
    [InlineData("ABCD123")]
    public async Task Placa_invalida_retorna_400(string placa)
    {
        var (cliente, _) = await _api.NovoCliente();

        var resposta = await (await _api.Atendente()).PostAsJsonAsync("/api/v1/veiculos",
            new CriarVeiculoRequest(cliente.Id, placa, "VW", "Gol", 2020));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Crud_de_veiculo_e_placa_duplicada()
    {
        var atendente = await _api.Atendente();
        var (cliente, _) = await _api.NovoCliente();
        var placa = ApiTeste.GerarPlaca();

        var veiculo = await ApiTeste.Ler<VeiculoResponse>(await atendente.PostAsJsonAsync("/api/v1/veiculos",
            new CriarVeiculoRequest(cliente.Id, placa, "VW", "Gol", 2020)), HttpStatusCode.Created);
        var duplicado = await atendente.PostAsJsonAsync("/api/v1/veiculos",
            new CriarVeiculoRequest(cliente.Id, placa, "VW", "Gol", 2020));
        var atualizado = await ApiTeste.Ler<VeiculoResponse>(await atendente.PutAsJsonAsync($"/api/v1/veiculos/{veiculo.Id}",
            new AtualizarVeiculoRequest(placa, "VW", "Polo", 2021)));
        var doCliente = await ApiTeste.Ler<List<VeiculoResponse>>(await atendente.GetAsync($"/api/v1/veiculos?clienteId={cliente.Id}"));
        var removido = await atendente.DeleteAsync($"/api/v1/veiculos/{veiculo.Id}");

        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        Assert.Equal("Polo", atualizado.Modelo);
        Assert.Single(doCliente);
        Assert.Equal(HttpStatusCode.NoContent, removido.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await atendente.GetAsync($"/api/v1/veiculos/{veiculo.Id}")).StatusCode);
    }

    [Fact]
    public async Task Entrada_de_estoque_aumenta_saldo()
    {
        var peca = await _api.NovaPeca(saldo: 3);

        var depois = await ApiTeste.Ler<PecaResponse>(await (await _api.Admin()).PostAsJsonAsync(
            $"/api/v1/pecas/{peca.Id}/entradas", new EntradaEstoqueRequest(7)));

        Assert.Equal(10, depois.Saldo);
    }

    [Fact]
    public async Task Mecanico_le_pecas_mas_nao_cadastra()
    {
        var mecanico = await _api.Mecanico();

        Assert.Equal(HttpStatusCode.OK, (await mecanico.GetAsync("/api/v1/pecas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await mecanico.PostAsJsonAsync("/api/v1/pecas", new { })).StatusCode);
    }
}
