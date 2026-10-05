using Oficina.Domain.Comum;

namespace Oficina.Domain.Veiculos;

public sealed class Veiculo : AggregateRoot
{
    public Guid ClienteId { get; private set; }
    public Placa Placa { get; private set; } = null!;
    public string Marca { get; private set; } = null!;
    public string Modelo { get; private set; } = null!;
    public int Ano { get; private set; }

    private Veiculo() { }

    public Veiculo(Guid clienteId, Placa placa, string marca, string modelo, int ano)
    {
        ClienteId = clienteId;
        Atualizar(placa, marca, modelo, ano);
    }

    public void Atualizar(Placa placa, string marca, string modelo, int ano)
    {
        if (ano < 1900 || ano > DateTime.UtcNow.Year + 1)
            throw new DadoInvalidoException("Ano do veículo inválido.");

        Placa = placa;
        Marca = Guarda.Obrigatorio(marca, "Marca");
        Modelo = Guarda.Obrigatorio(modelo, "Modelo");
        Ano = ano;
    }
}
