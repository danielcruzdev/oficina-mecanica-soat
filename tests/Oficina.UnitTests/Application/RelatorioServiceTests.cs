using NSubstitute;
using Oficina.Application.Abstracoes;
using Oficina.Application.OrdensDeServico;
using Oficina.Application.Relatorios;
using Oficina.Domain.Catalogo;
using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;
using Oficina.Domain.Estoque;
using Oficina.Domain.OrdensDeServico;
using Oficina.Domain.Veiculos;

namespace Oficina.UnitTests.Application;

public class RelatorioServiceTests
{
    private static readonly DateTimeOffset Inicio = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private readonly IOrdemDeServicoRepository _ordens = Substitute.For<IOrdemDeServicoRepository>();

    private static OrdemDeServico OsExecutada(int minutos)
    {
        var usuario = Guid.NewGuid();
        var os = OrdemDeServico.Abrir("OS-2026-000001", Guid.NewGuid(), Guid.NewGuid(), "X", usuario, Inicio);
        os.IniciarDiagnostico(usuario, Inicio);
        os.AdicionarServico(Guid.NewGuid(), "Alinhamento", Dinheiro.Criar(100m));
        os.EnviarOrcamento(usuario, Inicio);
        os.AprovarOrcamento(usuario, Inicio);
        os.FinalizarExecucao(usuario, Inicio.AddMinutes(minutos));
        return os;
    }

    [Fact]
    public async Task Calcula_media_minimo_e_maximo()
    {
        _ordens.ListarExecutadasAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns([OsExecutada(30), OsExecutada(60), OsExecutada(90)]);

        var resultado = await new RelatorioService(_ordens).TempoMedioExecucaoAsync(null, null, null);

        Assert.Equal(3, resultado.Quantidade);
        Assert.Equal(60, resultado.MediaMinutos);
        Assert.Equal(30, resultado.MinimoMinutos);
        Assert.Equal(90, resultado.MaximoMinutos);
    }

    [Fact]
    public async Task Sem_os_executadas_retorna_quantidade_zero()
    {
        var resultado = await new RelatorioService(_ordens).TempoMedioExecucaoAsync(Inicio, Inicio.AddDays(1), Guid.NewGuid());

        Assert.Equal(0, resultado.Quantidade);
        Assert.Null(resultado.MediaMinutos);
    }

    [Fact]
    public async Task Listagem_converte_filtros_e_normaliza_paginacao()
    {
        _ordens.ListarAsync(Arg.Any<FiltroOrdensDeServico>(), Arg.Any<CancellationToken>()).Returns([OsExecutada(10)]);
        var service = new OrdemDeServicoService(_ordens, Substitute.For<IClienteRepository>(),
            Substitute.For<IVeiculoRepository>(), Substitute.For<IServicoRepository>(),
            Substitute.For<IPecaRepository>(), Substitute.For<IUnitOfWork>(), TimeProvider.System);

        var lista = await service.ListarAsync(new ListarOrdensDeServicoRequest(
            StatusOS.Finalizada, "529.982.247-25", "abc-1d23", Inicio, null, 0, 1000));

        Assert.Single(lista);
        await _ordens.Received(1).ListarAsync(
            Arg.Is<FiltroOrdensDeServico>(f =>
                f.Status == StatusOS.Finalizada &&
                f.Documento!.Numero == "52998224725" &&
                f.Placa!.Valor == "ABC1D23" &&
                f.De == Inicio &&
                f.Pagina == 1 &&
                f.TamanhoPagina == 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Listagem_sem_filtros_nao_cria_documento_nem_placa()
    {
        var service = new OrdemDeServicoService(_ordens, Substitute.For<IClienteRepository>(),
            Substitute.For<IVeiculoRepository>(), Substitute.For<IServicoRepository>(),
            Substitute.For<IPecaRepository>(), Substitute.For<IUnitOfWork>(), TimeProvider.System);

        await service.ListarAsync(new ListarOrdensDeServicoRequest());

        await _ordens.Received(1).ListarAsync(
            Arg.Is<FiltroOrdensDeServico>(f => f.Status == null && f.Documento == null && f.Placa == null),
            Arg.Any<CancellationToken>());
    }
}
