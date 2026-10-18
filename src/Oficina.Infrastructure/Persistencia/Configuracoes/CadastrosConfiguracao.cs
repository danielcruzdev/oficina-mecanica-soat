using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oficina.Domain.Catalogo;
using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;
using Oficina.Domain.Identidade;
using Oficina.Domain.Veiculos;

namespace Oficina.Infrastructure.Persistencia.Configuracoes;

internal static class Conversoes
{
    public static PropertyBuilder<Dinheiro> ComoDinheiro(this PropertyBuilder<Dinheiro> propriedade) =>
        propriedade.HasConversion(d => d.Valor, v => Dinheiro.Criar(v)).HasPrecision(12, 2);
}

internal sealed class ClienteConfiguracao : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");
        builder.Ignore(c => c.Eventos);
        builder.Property(c => c.Documento)
            .HasConversion(d => d.Numero, v => Documento.Criar(v))
            .HasMaxLength(14);
        builder.HasIndex(c => c.Documento).IsUnique();
        builder.Property(c => c.Nome).HasMaxLength(150);
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.Telefone).HasMaxLength(20);
    }
}

internal sealed class VeiculoConfiguracao : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.ToTable("veiculos");
        builder.Ignore(v => v.Eventos);
        builder.Property(v => v.Placa)
            .HasConversion(p => p.Valor, v => Placa.Criar(v))
            .HasMaxLength(7);
        builder.HasIndex(v => v.Placa).IsUnique();
        builder.Property(v => v.Marca).HasMaxLength(60);
        builder.Property(v => v.Modelo).HasMaxLength(60);
        builder.HasOne<Cliente>().WithMany().HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ServicoConfiguracao : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        builder.ToTable("servicos");
        builder.Ignore(s => s.Eventos);
        builder.Property(s => s.Nome).HasMaxLength(100);
        builder.HasIndex(s => s.Nome).IsUnique();
        builder.Property(s => s.Descricao).HasMaxLength(500);
        builder.Property(s => s.PrecoMaoDeObra).ComoDinheiro();
    }
}

internal sealed class UsuarioConfiguracao : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.Ignore(u => u.Eventos);
        builder.Property(u => u.Login).HasMaxLength(150);
        builder.HasIndex(u => u.Login).IsUnique();
        builder.Property(u => u.SenhaHash).HasMaxLength(200);
        builder.HasOne<Cliente>().WithMany().HasForeignKey(u => u.ClienteId).OnDelete(DeleteBehavior.Restrict);
    }
}
