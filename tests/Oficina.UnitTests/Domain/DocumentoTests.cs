using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;

namespace Oficina.UnitTests.Domain;

public class DocumentoTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    [InlineData(" 529 982 247 25 ", "52998224725")]
    public void Cpf_valido_e_normalizado(string entrada, string esperado)
    {
        var documento = Documento.Criar(entrada);

        Assert.Equal(esperado, documento.Numero);
        Assert.Equal(TipoPessoa.Fisica, documento.Tipo);
    }

    [Theory]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("12.ABC.345/01DE-35", "12ABC34501DE35")]
    [InlineData("12abc34501de35", "12ABC34501DE35")]
    public void Cnpj_valido_numerico_e_alfanumerico(string entrada, string esperado)
    {
        var documento = Documento.Criar(entrada);

        Assert.Equal(esperado, documento.Numero);
        Assert.Equal(TipoPessoa.Juridica, documento.Tipo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("529.982.247-24")]
    [InlineData("111.111.111-11")]
    [InlineData("1234567890")]
    [InlineData("11.222.333/0001-82")]
    [InlineData("11.111.111/1111-11")]
    [InlineData("12.ABC.345/01DE-3A")]
    [InlineData("5299822472A")]
    [InlineData("abc")]
    public void Documento_invalido_lanca_excecao(string? entrada)
    {
        Assert.Throws<DadoInvalidoException>(() => Documento.Criar(entrada));
    }

    [Fact]
    public void Documentos_com_mesmo_numero_sao_iguais()
    {
        Assert.Equal(Documento.Criar("529.982.247-25"), Documento.Criar("52998224725"));
    }
}
