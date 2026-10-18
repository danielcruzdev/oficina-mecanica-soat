namespace Oficina.Application.Comum;

public class NaoEncontradoException(string mensagem) : Exception(mensagem);

public class ConflitoException(string mensagem) : Exception(mensagem);

public class CredenciaisInvalidasException() : Exception("Login ou senha inválidos.");
