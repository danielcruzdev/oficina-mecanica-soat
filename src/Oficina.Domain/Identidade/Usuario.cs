using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;

namespace Oficina.Domain.Identidade;

public enum Papel
{
    Administrador,
    Atendente,
    Mecanico,
    Cliente
}

public sealed class Usuario : AggregateRoot
{
    public string Login { get; private set; } = null!;
    public string SenhaHash { get; private set; } = null!;
    public Papel Papel { get; private set; }
    public Guid? ClienteId { get; private set; }
    public bool Ativo { get; private set; }

    private Usuario() { }

    private Usuario(string login, string senhaHash, Papel papel, Guid? clienteId)
    {
        Login = Guarda.Obrigatorio(login, "Login").ToLowerInvariant();
        SenhaHash = Guarda.Obrigatorio(senhaHash, "Senha");
        Papel = papel;
        ClienteId = clienteId;
        Ativo = true;
    }

    public static Usuario Funcionario(string login, string senhaHash, Papel papel) =>
        papel == Papel.Cliente
            ? throw new DadoInvalidoException("Use Usuario.DoCliente para criar o acesso de um cliente.")
            : new Usuario(login, senhaHash, papel, null);

    /// <summary>Acesso do cliente: o login é o próprio CPF/CNPJ.</summary>
    public static Usuario DoCliente(Cliente cliente, string senhaHash) =>
        new(cliente.Documento.Numero, senhaHash, Papel.Cliente, cliente.Id);

    public void Desativar() => Ativo = false;
}
