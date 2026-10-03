using Oficina.Domain.Comum;

namespace Oficina.Domain.Clientes;

public enum TipoPessoa
{
    Fisica,
    Juridica
}

/// <summary>CPF (11 dígitos) ou CNPJ (14 posições, numérico ou alfanumérico), com dígitos verificadores.</summary>
public sealed record Documento
{
    public string Numero { get; }

    public TipoPessoa Tipo => Numero.Length == 11 ? TipoPessoa.Fisica : TipoPessoa.Juridica;

    private Documento(string numero) => Numero = numero;

    public static Documento Criar(string? valor)
    {
        var numero = new string((valor ?? "").Where(c => !".-/ ".Contains(c)).ToArray()).ToUpperInvariant();

        var valido = numero.Length switch
        {
            11 => CpfValido(numero),
            14 => CnpjValido(numero),
            _ => false
        };

        if (!valido)
            throw new DadoInvalidoException("CPF/CNPJ inválido.");

        return new Documento(numero);
    }

    private static bool CpfValido(string cpf) =>
        cpf.All(char.IsAsciiDigit)
        && !TodosIguais(cpf)
        && cpf[9] - '0' == Digito(cpf[..9], int.MaxValue)
        && cpf[10] - '0' == Digito(cpf[..10], int.MaxValue);

    // CNPJ alfanumérico (IN RFB 2.229/2024): 12 posições [0-9A-Z] + 2 dígitos verificadores numéricos.
    // O valor de cada caractere é o seu código ASCII menos 48, o que mantém o cálculo do CNPJ numérico.
    private static bool CnpjValido(string cnpj) =>
        cnpj[..12].All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c))
        && cnpj[12..].All(char.IsAsciiDigit)
        && !TodosIguais(cnpj)
        && cnpj[12] - '0' == Digito(cnpj[..12], 9)
        && cnpj[13] - '0' == Digito(cnpj[..13], 9);

    // Módulo 11 com pesos 2, 3, 4... da direita para a esquerda, voltando a 2 após o peso máximo.
    private static int Digito(string baseCalculo, int pesoMaximo)
    {
        var soma = 0;
        var peso = 2;
        for (var i = baseCalculo.Length - 1; i >= 0; i--)
        {
            soma += (baseCalculo[i] - '0') * peso;
            peso = peso == pesoMaximo ? 2 : peso + 1;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static bool TodosIguais(string valor) => valor.All(c => c == valor[0]);

    public override string ToString() => Numero;
}
