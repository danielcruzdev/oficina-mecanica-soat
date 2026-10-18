namespace Oficina.Application.Abstracoes;

public interface ISenhaHasher
{
    string Gerar(string senha);
    bool Verificar(string hash, string senha);
}
