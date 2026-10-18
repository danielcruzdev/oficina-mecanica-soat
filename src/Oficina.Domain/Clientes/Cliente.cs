using Oficina.Domain.Comum;

namespace Oficina.Domain.Clientes;

public sealed class Cliente : AggregateRoot
{
    public Documento Documento { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Telefone { get; private set; }
    public bool Ativo { get; private set; }

    private Cliente() { }

    public Cliente(Documento documento, string nome, string? email, string? telefone)
    {
        Documento = documento;
        Ativo = true;
        Atualizar(nome, email, telefone);
    }

    public void Atualizar(string nome, string? email, string? telefone)
    {
        email = Guarda.Opcional(email);
        if (email is not null && !email.Contains('@'))
            throw new DadoInvalidoException("E-mail inválido.");

        Nome = Guarda.Obrigatorio(nome, "Nome");
        Email = email;
        Telefone = Guarda.Opcional(telefone);
    }

    public void Inativar() => Ativo = false;
}
