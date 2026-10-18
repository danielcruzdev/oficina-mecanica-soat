using Oficina.Domain.Catalogo;
using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;
using Oficina.Domain.Identidade;
using Oficina.Domain.Veiculos;

namespace Oficina.UnitTests.Domain;

public class CadastrosTests
{
    private static readonly Documento Cpf = Documento.Criar("52998224725");

    [Fact]
    public void Cliente_e_criado_ativo_e_pode_ser_inativado()
    {
        var cliente = new Cliente(Cpf, " Maria Silva ", "maria@email.com", "11999990000");

        Assert.Equal("Maria Silva", cliente.Nome);
        Assert.True(cliente.Ativo);

        cliente.Inativar();
        Assert.False(cliente.Ativo);
    }

    [Theory]
    [InlineData("", "a@b.com")]
    [InlineData("Maria", "email-invalido")]
    public void Cliente_com_dados_invalidos_lanca_excecao(string nome, string email)
    {
        Assert.Throws<DadoInvalidoException>(() => new Cliente(Cpf, nome, email, null));
    }

    [Fact]
    public void Cliente_com_email_e_telefone_vazios_guarda_nulo()
    {
        var cliente = new Cliente(Cpf, "Maria", " ", "");

        Assert.Null(cliente.Email);
        Assert.Null(cliente.Telefone);
    }

    [Fact]
    public void Veiculo_valido()
    {
        var veiculo = new Veiculo(Guid.NewGuid(), Placa.Criar("ABC1D23"), "VW", "Gol", 2020);

        Assert.Equal("ABC1D23", veiculo.Placa.Valor);
        Assert.Equal(2020, veiculo.Ano);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(3000)]
    public void Veiculo_com_ano_invalido_lanca_excecao(int ano)
    {
        Assert.Throws<DadoInvalidoException>(() =>
            new Veiculo(Guid.NewGuid(), Placa.Criar("ABC1D23"), "VW", "Gol", ano));
    }

    [Fact]
    public void Veiculo_aceita_ano_seguinte_ao_atual()
    {
        var ano = DateTime.UtcNow.Year + 1;

        Assert.Equal(ano, new Veiculo(Guid.NewGuid(), Placa.Criar("ABC1D23"), "VW", "Gol", ano).Ano);
    }

    [Fact]
    public void Veiculo_sem_marca_lanca_excecao()
    {
        Assert.Throws<DadoInvalidoException>(() =>
            new Veiculo(Guid.NewGuid(), Placa.Criar("ABC1D23"), "", "Gol", 2020));
    }

    [Fact]
    public void Servico_valido_e_inativacao()
    {
        var servico = new Servico("Alinhamento", null, Dinheiro.Criar(120m), 45);
        Assert.True(servico.Ativo);

        servico.Inativar();
        Assert.False(servico.Ativo);
    }

    [Fact]
    public void Servico_com_tempo_estimado_zero_lanca_excecao()
    {
        Assert.Throws<DadoInvalidoException>(() => new Servico("Alinhamento", null, Dinheiro.Criar(120m), 0));
    }

    [Fact]
    public void Usuario_do_cliente_usa_documento_como_login()
    {
        var cliente = new Cliente(Cpf, "Maria", null, null);

        var usuario = Usuario.DoCliente(cliente, "hash");

        Assert.Equal("52998224725", usuario.Login);
        Assert.Equal(Papel.Cliente, usuario.Papel);
        Assert.Equal(cliente.Id, usuario.ClienteId);
    }

    [Fact]
    public void Funcionario_nao_pode_ter_papel_cliente()
    {
        Assert.Throws<DadoInvalidoException>(() => Usuario.Funcionario("x@oficina.local", "hash", Papel.Cliente));
    }

    [Fact]
    public void Funcionario_tem_login_minusculo_e_pode_ser_desativado()
    {
        var usuario = Usuario.Funcionario("Admin@Oficina.local", "hash", Papel.Administrador);
        Assert.Equal("admin@oficina.local", usuario.Login);

        usuario.Desativar();
        Assert.False(usuario.Ativo);
    }
}
