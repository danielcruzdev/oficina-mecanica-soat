using Oficina.Domain.Comum;
using Oficina.Domain.Estoque;

namespace Oficina.UnitTests.Domain;

public class PecaTests
{
    private static readonly DateTimeOffset Agora = DateTimeOffset.UtcNow;

    private static Peca NovaPeca(int saldo = 10, int minimo = 2) =>
        new("flt-001", "Filtro de óleo", TipoItem.Peca, "un", Dinheiro.Criar(35m), minimo, saldo, Agora);

    [Fact]
    public void Cadastro_com_saldo_inicial_gera_movimentacao_de_entrada()
    {
        var peca = NovaPeca(saldo: 10);

        Assert.Equal("FLT-001", peca.Sku);
        Assert.Equal(10, peca.Saldo);
        var mov = Assert.Single(peca.Movimentacoes);
        Assert.Equal(TipoMovimentacao.Entrada, mov.Tipo);
        Assert.Equal(10, mov.Quantidade);
        Assert.Contains(peca.Eventos, e => e is PecaCadastrada);
    }

    [Fact]
    public void Cadastro_sem_saldo_nao_gera_movimentacao()
    {
        Assert.Empty(NovaPeca(saldo: 0).Movimentacoes);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Saldo_ou_minimo_negativo_lanca_dado_invalido(int saldo, int minimo)
    {
        Assert.Throws<DadoInvalidoException>(() => NovaPeca(saldo, minimo));
    }

    [Fact]
    public void Entrada_aumenta_saldo()
    {
        var peca = NovaPeca(saldo: 0);

        peca.RegistrarEntrada(5, Agora);

        Assert.Equal(5, peca.Saldo);
        Assert.Contains(peca.Eventos, e => e is EntradaDeEstoqueRegistrada { Quantidade: 5 });
    }

    [Fact]
    public void Entrada_com_quantidade_zero_lanca_dado_invalido()
    {
        Assert.Throws<DadoInvalidoException>(() => NovaPeca().RegistrarEntrada(0, Agora));
    }

    [Fact]
    public void Baixa_por_os_reduz_saldo_e_registra_saida()
    {
        var peca = NovaPeca(saldo: 10, minimo: 2);
        var osId = Guid.NewGuid();

        peca.BaixarPorOS(3, osId, Agora);

        Assert.Equal(7, peca.Saldo);
        var saida = peca.Movimentacoes.Last();
        Assert.Equal(TipoMovimentacao.SaidaOS, saida.Tipo);
        Assert.Equal(osId, saida.OrdemDeServicoId);
        Assert.DoesNotContain(peca.Eventos, e => e is EstoqueAbaixoDoMinimo);
    }

    [Fact]
    public void Baixa_que_atinge_o_minimo_emite_alerta()
    {
        var peca = NovaPeca(saldo: 5, minimo: 2);

        peca.BaixarPorOS(3, Guid.NewGuid(), Agora);

        Assert.Contains(peca.Eventos, e => e is EstoqueAbaixoDoMinimo { Saldo: 2 });
    }

    [Fact]
    public void Baixa_maior_que_o_saldo_lanca_regra_de_negocio_e_nao_altera_saldo()
    {
        var peca = NovaPeca(saldo: 2);

        Assert.Throws<DomainException>(() => peca.BaixarPorOS(3, Guid.NewGuid(), Agora));
        Assert.Equal(2, peca.Saldo);
        Assert.False(peca.TemDisponivel(3));
    }

    [Fact]
    public void Atualizar_e_inativar()
    {
        var peca = NovaPeca();

        peca.Atualizar("Óleo 5W30", TipoItem.Insumo, "L", Dinheiro.Criar(45m), 10);
        peca.Inativar();

        Assert.Equal("Óleo 5W30", peca.Nome);
        Assert.Equal(TipoItem.Insumo, peca.Tipo);
        Assert.False(peca.Ativo);
    }
}
