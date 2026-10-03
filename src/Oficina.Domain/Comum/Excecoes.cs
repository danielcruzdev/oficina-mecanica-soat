namespace Oficina.Domain.Comum;

/// <summary>Regra de negócio violada.</summary>
public class DomainException(string mensagem) : Exception(mensagem);

/// <summary>Dado de entrada inválido (CPF, placa, campo obrigatório...).</summary>
public class DadoInvalidoException(string mensagem) : DomainException(mensagem);

/// <summary>Mudança de status da OS fora da matriz de transições.</summary>
public class TransicaoDeStatusInvalidaException(string mensagem) : DomainException(mensagem);
