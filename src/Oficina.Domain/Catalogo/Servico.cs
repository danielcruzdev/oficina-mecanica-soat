using Oficina.Domain.Comum;

namespace Oficina.Domain.Catalogo;

public sealed class Servico : AggregateRoot
{
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public Dinheiro PrecoMaoDeObra { get; private set; } = null!;
    public int TempoEstimadoMinutos { get; private set; }
    public bool Ativo { get; private set; }

    private Servico() { }

    public Servico(string nome, string? descricao, Dinheiro precoMaoDeObra, int tempoEstimadoMinutos)
    {
        Ativo = true;
        Atualizar(nome, descricao, precoMaoDeObra, tempoEstimadoMinutos);
    }

    public void Atualizar(string nome, string? descricao, Dinheiro precoMaoDeObra, int tempoEstimadoMinutos)
    {
        if (tempoEstimadoMinutos <= 0)
            throw new DadoInvalidoException("Tempo estimado deve ser maior que zero.");

        Nome = Guarda.Obrigatorio(nome, "Nome");
        Descricao = Guarda.Opcional(descricao);
        PrecoMaoDeObra = precoMaoDeObra;
        TempoEstimadoMinutos = tempoEstimadoMinutos;
    }

    public void Inativar() => Ativo = false;
}
