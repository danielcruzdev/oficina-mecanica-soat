using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Catalogo;
using Oficina.Domain.Clientes;
using Oficina.Domain.Estoque;
using Oficina.Domain.OrdensDeServico;
using Oficina.Domain.Veiculos;

namespace Oficina.Infrastructure.Persistencia.Configuracoes;

internal sealed class OrdemDeServicoConfiguracao : IEntityTypeConfiguration<OrdemDeServico>
{
    public const string SequenciaNumero = "numero_os_seq";

    public void Configure(EntityTypeBuilder<OrdemDeServico> builder)
    {
        builder.ToTable("ordens_servico");
        builder.Ignore(o => o.Eventos);
        builder.Property(o => o.Numero).HasMaxLength(20);
        builder.HasIndex(o => o.Numero).IsUnique();
        builder.HasIndex(o => o.Status);
        builder.Property(o => o.DescricaoProblema).HasMaxLength(1000);
        builder.HasOne<Cliente>().WithMany().HasForeignKey(o => o.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Veiculo>().WithMany().HasForeignKey(o => o.VeiculoId).OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(o => o.Orcamento, orcamento =>
        {
            orcamento.Property(x => x.TotalServicos).ComoDinheiro();
            orcamento.Property(x => x.TotalPecas).ComoDinheiro();
            orcamento.Property(x => x.Total).ComoDinheiro();
            orcamento.Property(x => x.MotivoRecusa).HasMaxLength(500);
        });
        builder.Navigation(o => o.Orcamento).IsRequired();

        builder.HasMany(o => o.Servicos).WithOne().HasForeignKey("OrdemDeServicoId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Pecas).WithOne().HasForeignKey("OrdemDeServicoId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Historico).WithOne().HasForeignKey("OrdemDeServicoId").OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ItemServicoConfiguracao : IEntityTypeConfiguration<ItemServico>
{
    public void Configure(EntityTypeBuilder<ItemServico> builder)
    {
        builder.ToTable("os_itens_servico");
        builder.Property(i => i.Nome).HasMaxLength(100);
        builder.Property(i => i.Preco).ComoDinheiro();
        builder.HasOne<Servico>().WithMany().HasForeignKey(i => i.ServicoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ItemPecaConfiguracao : IEntityTypeConfiguration<ItemPeca>
{
    public void Configure(EntityTypeBuilder<ItemPeca> builder)
    {
        builder.ToTable("os_itens_peca");
        builder.Property(i => i.Nome).HasMaxLength(100);
        builder.Property(i => i.PrecoUnitario).ComoDinheiro();
        builder.HasOne<Peca>().WithMany().HasForeignKey(i => i.PecaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class HistoricoStatusConfiguracao : IEntityTypeConfiguration<HistoricoStatus>
{
    public void Configure(EntityTypeBuilder<HistoricoStatus> builder)
    {
        builder.ToTable("os_historico_status");
    }
}
