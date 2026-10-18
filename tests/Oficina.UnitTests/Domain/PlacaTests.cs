using Oficina.Domain.Comum;
using Oficina.Domain.Veiculos;

namespace Oficina.UnitTests.Domain;

public class PlacaTests
{
    [Theory]
    [InlineData("ABC1234", "ABC1234")]
    [InlineData("abc-1234", "ABC1234")]
    [InlineData("ABC1D23", "ABC1D23")]
    [InlineData(" bra2e19 ", "BRA2E19")]
    public void Placa_antiga_e_mercosul_validas(string entrada, string esperado)
    {
        Assert.Equal(esperado, Placa.Criar(entrada).Valor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AB12345")]
    [InlineData("ABCD123")]
    [InlineData("ABC12D3")]
    [InlineData("ABC123")]
    [InlineData("ABC12345")]
    public void Placa_invalida_lanca_excecao(string? entrada)
    {
        Assert.Throws<DadoInvalidoException>(() => Placa.Criar(entrada));
    }
}
