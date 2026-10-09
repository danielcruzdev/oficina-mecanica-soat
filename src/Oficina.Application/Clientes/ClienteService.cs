using Oficina.Application.Abstracoes;
using Oficina.Application.Comum;
using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;
using Oficina.Domain.Identidade;

namespace Oficina.Application.Clientes;

/// <param name="Senha">Opcional. Se informada, cria o acesso do cliente à API (login = CPF/CNPJ).</param>
public sealed record CriarClienteRequest(string Documento, string Nome, string? Email, string? Telefone, string? Senha);

public sealed record AtualizarClienteRequest(string Nome, string? Email, string? Telefone);

public sealed record ClienteResponse(Guid Id, string Documento, TipoPessoa TipoPessoa, string Nome, string? Email,
    string? Telefone, bool Ativo)
{
    public static ClienteResponse De(Cliente c) =>
        new(c.Id, c.Documento.Numero, c.Documento.Tipo, c.Nome, c.Email, c.Telefone, c.Ativo);
}

public sealed class ClienteService(
    IClienteRepository clientes,
    IUsuarioRepository usuarios,
    ISenhaHasher hasher,
    IUnitOfWork unitOfWork)
{
    public const int TamanhoMinimoSenha = 6;

    public async Task<ClienteResponse> CriarAsync(CriarClienteRequest request, CancellationToken ct = default)
    {
        var documento = Documento.Criar(request.Documento);
        if (await clientes.ObterPorDocumentoAsync(documento, ct) is not null)
            throw new ConflitoException("Já existe um cliente com este CPF/CNPJ.");

        var cliente = new Cliente(documento, request.Nome, request.Email, request.Telefone);
        clientes.Adicionar(cliente);

        if (!string.IsNullOrEmpty(request.Senha))
        {
            if (request.Senha.Length < TamanhoMinimoSenha)
                throw new DadoInvalidoException($"A senha deve ter pelo menos {TamanhoMinimoSenha} caracteres.");

            usuarios.Adicionar(Usuario.DoCliente(cliente, hasher.Gerar(request.Senha)));
        }

        await unitOfWork.SalvarAsync(ct);
        return ClienteResponse.De(cliente);
    }

    public async Task<ClienteResponse> ObterAsync(Guid id, CancellationToken ct = default) =>
        ClienteResponse.De(await Carregar(id, ct));

    public async Task<ClienteResponse> ObterPorDocumentoAsync(string documento, CancellationToken ct = default) =>
        ClienteResponse.De(await clientes.ObterPorDocumentoAsync(Documento.Criar(documento), ct)
            ?? throw new NaoEncontradoException("Cliente não encontrado."));

    public async Task<IReadOnlyList<ClienteResponse>> ListarAsync(int? pagina, int? tamanhoPagina,
        CancellationToken ct = default)
    {
        var (p, t) = Paginacao.Normalizar(pagina, tamanhoPagina);
        return (await clientes.ListarAsync(p, t, ct)).Select(ClienteResponse.De).ToList();
    }

    public async Task<ClienteResponse> AtualizarAsync(Guid id, AtualizarClienteRequest request,
        CancellationToken ct = default)
    {
        var cliente = await Carregar(id, ct);
        cliente.Atualizar(request.Nome, request.Email, request.Telefone);
        await unitOfWork.SalvarAsync(ct);
        return ClienteResponse.De(cliente);
    }

    /// <summary>Remoção lógica: o histórico de OS do cliente é preservado.</summary>
    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        var cliente = await Carregar(id, ct);
        cliente.Inativar();
        await unitOfWork.SalvarAsync(ct);
    }

    private async Task<Cliente> Carregar(Guid id, CancellationToken ct) =>
        await clientes.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Cliente não encontrado.");
}
