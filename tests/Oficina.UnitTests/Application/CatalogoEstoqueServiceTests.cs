using NSubstitute;
using Oficina.Application.Abstracoes;
using Oficina.Application.Catalogo;
using Oficina.Application.Comum;
using Oficina.Application.Estoque;
using Oficina.Domain.Catalogo;
using Oficina.Domain.Comum;
using Oficina.Domain.Estoque;

namespace Oficina.UnitTests.Application;

public class CatalogoEstoqueServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Servico_crud_completo()
    {
        var repo = Substitute.For<IServicoRepository>();
        var service = new ServicoService(repo, _uow);

        var criado = await service.CriarAsync(new ServicoRequest("Alinhamento", null, 120m, 45));
        repo.Received(1).Adicionar(Arg.Any<Servico>());

        var servico = new Servico("Alinhamento", null, Dinheiro.Criar(120m), 45);
        repo.ObterPorIdAsync(servico.Id, Arg.Any<CancellationToken>()).Returns(servico);
        repo.ListarAsync(1, 20, Arg.Any<CancellationToken>()).Returns([servico]);

        var atualizado = await service.AtualizarAsync(servico.Id, new ServicoRequest("Alinhamento 3D", "x", 150m, 50));
        await service.RemoverAsync(servico.Id);

        Assert.Equal(120m, criado.PrecoMaoDeObra);
        Assert.Equal("Alinhamento 3D", atualizado.Nome);
        Assert.False((await service.ObterAsync(servico.Id)).Ativo);
        Assert.Single(await service.ListarAsync(null, null));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => service.ObterAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Servico_com_preco_negativo_lanca_dado_invalido()
    {
        var service = new ServicoService(Substitute.For<IServicoRepository>(), _uow);

        await Assert.ThrowsAsync<DadoInvalidoException>(() => service.CriarAsync(new ServicoRequest("X", null, -1m, 10)));
    }

    [Fact]
    public async Task Peca_crud_e_entrada_de_estoque()
    {
        var repo = Substitute.For<IPecaRepository>();
        var service = new PecaService(repo, _uow, TimeProvider.System);

        var criada = await service.CriarAsync(new CriarPecaRequest("flt-1", "Filtro", TipoItem.Peca, "un", 35m, 2, 10));
        repo.Received(1).Adicionar(Arg.Any<Peca>());

        var peca = new Peca("FLT-1", "Filtro", TipoItem.Peca, "un", Dinheiro.Criar(35m), 2, 10, DateTimeOffset.UtcNow);
        repo.ObterPorIdAsync(peca.Id, Arg.Any<CancellationToken>()).Returns(peca);
        repo.ListarAsync(1, 20, Arg.Any<CancellationToken>()).Returns([peca]);

        var comEntrada = await service.RegistrarEntradaAsync(peca.Id, new EntradaEstoqueRequest(5));
        var atualizada = await service.AtualizarAsync(peca.Id, new AtualizarPecaRequest("Filtro X", TipoItem.Peca, "un", 40m, 3));
        await service.RemoverAsync(peca.Id);

        Assert.Equal("FLT-1", criada.Sku);
        Assert.Equal(15, comEntrada.Saldo);
        Assert.Equal(40m, atualizada.Preco);
        Assert.False((await service.ObterAsync(peca.Id)).Ativo);
        Assert.Single(await service.ListarAsync(null, null));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => service.ObterAsync(Guid.NewGuid()));
    }
}
