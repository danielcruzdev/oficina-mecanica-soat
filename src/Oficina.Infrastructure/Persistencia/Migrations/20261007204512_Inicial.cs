using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "numero_os_seq");

            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    documento = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pecas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    unidade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    preco = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    saldo = table.Column<int>(type: "integer", nullable: false),
                    estoque_minimo = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pecas", x => x.id);
                    table.CheckConstraint("ck_pecas_saldo_nao_negativo", "saldo >= 0");
                });

            migrationBuilder.CreateTable(
                name: "servicos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    preco_mao_de_obra = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    tempo_estimado_minutos = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servicos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    login = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    papel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuarios_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "veiculos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    placa = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    marca = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    modelo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ano = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_veiculos", x => x.id);
                    table.ForeignKey(
                        name: "fk_veiculos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimentacoes_estoque",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    peca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    ordem_de_servico_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimentacoes_estoque", x => x.id);
                    table.ForeignKey(
                        name: "fk_movimentacoes_estoque_pecas_peca_id",
                        column: x => x.peca_id,
                        principalTable: "pecas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ordens_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    veiculo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao_problema = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    aberta_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inicio_execucao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fim_execucao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    entregue_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resultado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    orcamento_versao = table.Column<int>(type: "integer", nullable: false),
                    orcamento_total_servicos = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    orcamento_total_pecas = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    orcamento_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    orcamento_situacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    orcamento_enviado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    orcamento_decidido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    orcamento_motivo_recusa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ordens_servico", x => x.id);
                    table.ForeignKey(
                        name: "fk_ordens_servico_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ordens_servico_veiculos_veiculo_id",
                        column: x => x.veiculo_id,
                        principalTable: "veiculos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "os_historico_status",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    de = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    para = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ocorrido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ordem_de_servico_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_os_historico_status", x => x.id);
                    table.ForeignKey(
                        name: "fk_os_historico_status_ordens_de_servico_ordem_de_servico_id",
                        column: x => x.ordem_de_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "os_itens_peca",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    peca_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    preco_unitario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ordem_de_servico_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_os_itens_peca", x => x.id);
                    table.ForeignKey(
                        name: "fk_os_itens_peca_ordens_de_servico_ordem_de_servico_id",
                        column: x => x.ordem_de_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_os_itens_peca_pecas_peca_id",
                        column: x => x.peca_id,
                        principalTable: "pecas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "os_itens_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    preco = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ordem_de_servico_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_os_itens_servico", x => x.id);
                    table.ForeignKey(
                        name: "fk_os_itens_servico_ordens_de_servico_ordem_de_servico_id",
                        column: x => x.ordem_de_servico_id,
                        principalTable: "ordens_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_os_itens_servico_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clientes_documento",
                table: "clientes",
                column: "documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_movimentacoes_estoque_peca_id",
                table: "movimentacoes_estoque",
                column: "peca_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_cliente_id",
                table: "ordens_servico",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_numero",
                table: "ordens_servico",
                column: "numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_status",
                table: "ordens_servico",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_ordens_servico_veiculo_id",
                table: "ordens_servico",
                column: "veiculo_id");

            migrationBuilder.CreateIndex(
                name: "ix_os_historico_status_ordem_de_servico_id",
                table: "os_historico_status",
                column: "ordem_de_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_os_itens_peca_ordem_de_servico_id",
                table: "os_itens_peca",
                column: "ordem_de_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_os_itens_peca_peca_id",
                table: "os_itens_peca",
                column: "peca_id");

            migrationBuilder.CreateIndex(
                name: "ix_os_itens_servico_ordem_de_servico_id",
                table: "os_itens_servico",
                column: "ordem_de_servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_os_itens_servico_servico_id",
                table: "os_itens_servico",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_pecas_sku",
                table: "pecas",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_servicos_nome",
                table: "servicos",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_cliente_id",
                table: "usuarios",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_login",
                table: "usuarios",
                column: "login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_cliente_id",
                table: "veiculos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_placa",
                table: "veiculos",
                column: "placa",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimentacoes_estoque");

            migrationBuilder.DropTable(
                name: "os_historico_status");

            migrationBuilder.DropTable(
                name: "os_itens_peca");

            migrationBuilder.DropTable(
                name: "os_itens_servico");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "pecas");

            migrationBuilder.DropTable(
                name: "ordens_servico");

            migrationBuilder.DropTable(
                name: "servicos");

            migrationBuilder.DropTable(
                name: "veiculos");

            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropSequence(
                name: "numero_os_seq");
        }
    }
}
