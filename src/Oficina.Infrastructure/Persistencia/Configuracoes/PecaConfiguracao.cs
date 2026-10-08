using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Estoque;

namespace Oficina.Infrastructure.Persistencia.Configuracoes;

internal sealed class PecaConfiguracao : IEntityTypeConfiguration<Peca>
{
    public void Configure(EntityTypeBuilder<Peca> builder)
    {
        builder.ToTable("pecas");
        builder.Ignore(p => p.Eventos);
        builder.Property(p => p.Sku).HasMaxLength(30);
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.Property(p => p.Nome).HasMaxLength(100);
        builder.Property(p => p.Unidade).HasMaxLength(10);
        builder.Property(p => p.Preco).ComoDinheiro();
        builder.ToTable(t => t.HasCheckConstraint("ck_pecas_saldo_nao_negativo", "saldo >= 0"));

        // Concorrência otimista: duas aprovações disputando a mesma peça -> uma recebe 409.
        builder.Property(p => p.Versao).IsRowVersion().HasColumnName("xmin");

        builder.HasMany(p => p.Movimentacoes).WithOne().HasForeignKey(m => m.PecaId);
    }
}

internal sealed class MovimentacaoEstoqueConfiguracao : IEntityTypeConfiguration<MovimentacaoEstoque>
{
    public void Configure(EntityTypeBuilder<MovimentacaoEstoque> builder)
    {
        builder.ToTable("movimentacoes_estoque");
    }
}
