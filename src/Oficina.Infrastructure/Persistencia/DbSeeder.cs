using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstracoes;
using Oficina.Domain.Catalogo;
using Oficina.Domain.Clientes;
using Oficina.Domain.Comum;
using Oficina.Domain.Estoque;
using Oficina.Domain.Identidade;
using Oficina.Domain.Veiculos;

namespace Oficina.Infrastructure.Persistencia;

/// <summary>Dados de demonstração (ambiente local). As senhas abaixo NÃO são segredos reais.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(OficinaDbContext db, ISenhaHasher hasher, TimeProvider relogio)
    {
        if (await db.Usuarios.AnyAsync())
            return;

        var agora = relogio.GetUtcNow();

        db.Usuarios.AddRange(
            Usuario.Funcionario("admin@oficina.local", hasher.Gerar("Admin@123"), Papel.Administrador),
            Usuario.Funcionario("atendente@oficina.local", hasher.Gerar("Atendente@123"), Papel.Atendente),
            Usuario.Funcionario("mecanico@oficina.local", hasher.Gerar("Mecanico@123"), Papel.Mecanico));

        db.Servicos.AddRange(
            new Servico("Troca de óleo", "Troca de óleo do motor", Dinheiro.Criar(80m), 30),
            new Servico("Alinhamento", "Alinhamento de direção", Dinheiro.Criar(120m), 45),
            new Servico("Balanceamento", "Balanceamento das 4 rodas", Dinheiro.Criar(100m), 40),
            new Servico("Revisão de freios", "Inspeção e troca de pastilhas", Dinheiro.Criar(150m), 60));

        db.Pecas.AddRange(
            new Peca("FLT-001", "Filtro de óleo", TipoItem.Peca, "un", Dinheiro.Criar(35m), 5, 20, agora),
            new Peca("OLE-5W30", "Óleo 5W30 sintético", TipoItem.Insumo, "L", Dinheiro.Criar(45m), 10, 50, agora),
            new Peca("PST-001", "Pastilha de freio dianteira", TipoItem.Peca, "jogo", Dinheiro.Criar(150m), 2, 8, agora));

        var cliente = new Cliente(Documento.Criar("529.982.247-25"), "Cliente Demonstração", "cliente@email.com",
            "11999990000");
        db.Clientes.Add(cliente);
        db.Usuarios.Add(Usuario.DoCliente(cliente, hasher.Gerar("Cliente@123")));
        db.Veiculos.Add(new Veiculo(cliente.Id, Placa.Criar("ABC1D23"), "Volkswagen", "Gol", 2020));

        await db.SaveChangesAsync();
    }
}
