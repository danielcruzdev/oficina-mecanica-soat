using Oficina.Domain.Comum;

namespace Oficina.UnitTests.Domain;

public class DinheiroTests
{
    [Fact]
    public void Arredonda_para_duas_casas()
    {
        Assert.Equal(10.13m, Dinheiro.Criar(10.125m).Valor);
    }

    [Fact]
    public void Valor_negativo_lanca_excecao()
    {
        Assert.Throws<DadoInvalidoException>(() => Dinheiro.Criar(-0.01m));
    }

    [Fact]
    public void Soma_e_multiplicacao()
    {
        var total = Dinheiro.Criar(10m) + Dinheiro.Criar(2.5m) * 3;

        Assert.Equal(Dinheiro.Criar(17.5m), total);
    }

    [Fact]
    public void Zero_vale_zero()
    {
        Assert.Equal(0m, Dinheiro.Zero.Valor);
    }
}
