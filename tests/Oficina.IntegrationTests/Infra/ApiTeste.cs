using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Oficina.Application.Abstracoes;
using Oficina.Application.Catalogo;
using Oficina.Application.Clientes;
using Oficina.Application.Estoque;
using Oficina.Application.OrdensDeServico;
using Oficina.Domain.Estoque;

namespace Oficina.IntegrationTests.Infra;

/// <summary>Atalhos para os testes: login por papel, cadastros e geração de CPF/placa únicos.</summary>
public sealed class ApiTeste(OficinaApiFactory factory)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public HttpClient Anonimo() => factory.CreateClient();

    public Task<HttpClient> Admin() => Funcionario("admin@oficina.local", "Admin@123");
    public Task<HttpClient> Atendente() => Funcionario("atendente@oficina.local", "Atendente@123");
    public Task<HttpClient> Mecanico() => Funcionario("mecanico@oficina.local", "Mecanico@123");

    public Task<HttpClient> Funcionario(string login, string senha) =>
        Autenticado("/api/v1/auth/login", new { login, senha });

    public Task<HttpClient> Cliente(string documento, string senha) =>
        Autenticado("/api/v1/auth/clientes/login", new { documento, senha });

    private async Task<HttpClient> Autenticado(string rota, object credenciais)
    {
        var resposta = await Anonimo().PostAsJsonAsync(rota, credenciais);
        resposta.EnsureSuccessStatusCode();
        var token = await resposta.Content.ReadFromJsonAsync<TokenGerado>(Json);

        var client = Anonimo();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    public async Task<(ClienteResponse Cliente, string Senha)> NovoCliente()
    {
        const string senha = "Senha@123";
        var resposta = await (await Atendente()).PostAsJsonAsync("/api/v1/clientes",
            new CriarClienteRequest(GerarCpf(), "Cliente Teste", "teste@email.com", null, senha));
        return (await Ler<ClienteResponse>(resposta, HttpStatusCode.Created), senha);
    }

    public async Task<PecaResponse> NovaPeca(int saldo, decimal preco = 50m)
    {
        var resposta = await (await Admin()).PostAsJsonAsync("/api/v1/pecas",
            new CriarPecaRequest($"T-{Guid.NewGuid():N}"[..20], "Peça de teste", TipoItem.Peca, "un", preco, 0, saldo));
        return await Ler<PecaResponse>(resposta, HttpStatusCode.Created);
    }

    public async Task<ServicoResponse> ServicoDoSeed() =>
        (await (await Mecanico()).GetFromJsonAsync<List<ServicoResponse>>("/api/v1/servicos", Json))!
        .First(s => s.Nome == "Alinhamento");

    public async Task<PecaResponse> Peca(Guid id) =>
        (await (await Mecanico()).GetFromJsonAsync<PecaResponse>($"/api/v1/pecas/{id}", Json))!;

    /// <summary>Abre a OS, faz o diagnóstico, inclui serviço + peça e envia o orçamento.</summary>
    public async Task<OrdemDeServicoResponse> OsAguardandoAprovacao(ClienteResponse cliente, Guid pecaId,
        int quantidade)
    {
        var atendente = await Atendente();
        var mecanico = await Mecanico();

        var os = await Ler<OrdemDeServicoResponse>(await atendente.PostAsJsonAsync("/api/v1/ordens-servico",
            new AbrirOrdemDeServicoRequest(cliente.Documento,
                new VeiculoDaOrdemRequest(GerarPlaca(), "Fiat", "Uno", 2018), "Barulho no motor")),
            HttpStatusCode.Created);

        await Ler<OrdemDeServicoResponse>(await mecanico.PostAsync($"/api/v1/ordens-servico/{os.Id}/diagnostico", null));
        await Ler<OrcamentoResponse>(await mecanico.PostAsJsonAsync($"/api/v1/ordens-servico/{os.Id}/servicos",
            new AdicionarServicoRequest((await ServicoDoSeed()).Id)));
        await Ler<OrcamentoResponse>(await mecanico.PostAsJsonAsync($"/api/v1/ordens-servico/{os.Id}/pecas",
            new AdicionarPecaRequest(pecaId, quantidade)));

        return await Ler<OrdemDeServicoResponse>(
            await atendente.PostAsync($"/api/v1/ordens-servico/{os.Id}/orcamento/envio", null));
    }

    public static async Task<T> Ler<T>(HttpResponseMessage resposta, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.True(resposta.StatusCode == esperado, $"Esperado {esperado}, veio {resposta.StatusCode}: {corpo}");
        return JsonSerializer.Deserialize<T>(corpo, Json)!;
    }

    public static string GerarCpf()
    {
        var numeros = Enumerable.Range(0, 9).Select(_ => Random.Shared.Next(10)).ToList();
        for (var tamanho = 9; tamanho < 11; tamanho++)
        {
            var soma = numeros.Select((n, i) => n * (tamanho + 1 - i)).Sum();
            var resto = soma % 11;
            numeros.Add(resto < 2 ? 0 : 11 - resto);
        }

        return string.Concat(numeros);
    }

    public static string GerarPlaca()
    {
        const string letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char Letra() => letras[Random.Shared.Next(letras.Length)];
        return $"{Letra()}{Letra()}{Letra()}{Random.Shared.Next(10)}{Letra()}{Random.Shared.Next(10, 100)}";
    }
}
