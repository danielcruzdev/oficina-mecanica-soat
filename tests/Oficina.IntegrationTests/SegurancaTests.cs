using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Oficina.IntegrationTests.Infra;

namespace Oficina.IntegrationTests;

[Collection(ApiCollection.Nome)]
public class SegurancaTests(OficinaApiFactory factory)
{
    private readonly ApiTeste _api = new(factory);

    [Theory]
    [InlineData("/api/v1/clientes")]
    [InlineData("/api/v1/ordens-servico")]
    [InlineData("/api/v1/me/ordens-servico")]
    [InlineData("/api/v1/relatorios/tempo-medio-execucao")]
    public async Task Sem_token_retorna_401(string rota)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.Anonimo().GetAsync(rota)).StatusCode);
    }

    [Fact]
    public async Task Token_invalido_retorna_401()
    {
        var client = _api.Anonimo();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "abc.def.ghi");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/clientes")).StatusCode);
    }

    [Theory]
    [InlineData("admin@oficina.local", "senha-errada")]
    [InlineData("ninguem@oficina.local", "Admin@123")]
    public async Task Login_invalido_retorna_401(string login, string senha)
    {
        var resposta = await _api.Anonimo().PostAsJsonAsync("/api/v1/auth/login", new { login, senha });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Mecanico_nao_cadastra_cliente_403()
    {
        var resposta = await (await _api.Mecanico()).PostAsJsonAsync("/api/v1/clientes", new { });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Atendente_nao_ve_relatorio_403()
    {
        var resposta = await (await _api.Atendente()).GetAsync("/api/v1/relatorios/tempo-medio-execucao");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Cliente_nao_acessa_rotas_administrativas_403()
    {
        var (cliente, senha) = await _api.NovoCliente();
        var client = await _api.Cliente(cliente.Documento, senha);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/ordens-servico")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/clientes")).StatusCode);
    }

    [Fact]
    public async Task Funcionario_nao_acessa_area_do_cliente_403()
    {
        Assert.Equal(HttpStatusCode.Forbidden,
            (await (await _api.Admin()).GetAsync("/api/v1/me/ordens-servico")).StatusCode);
    }

    [Fact]
    public async Task Health_e_openapi_sao_publicos()
    {
        var client = _api.Anonimo();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        var openApi = await client.GetStringAsync("/openapi/v1.json");
        Assert.Contains("\"bearer\"", openApi);
    }

    [Fact]
    public async Task Erro_nao_expoe_stack_trace()
    {
        var resposta = await (await _api.Atendente()).PostAsJsonAsync("/api/v1/clientes",
            new { documento = "111.111.111-11", nome = "X" });

        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("traceId", corpo);
        Assert.DoesNotContain("   at ", corpo);
    }
}
