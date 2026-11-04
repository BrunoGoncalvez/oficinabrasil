namespace OficinaBrasil.Domain;

public enum ServiceOrderStatus
{
    RECEBIDA = 0, 
    EM_DIAGNOSTICO = 1,
    AGUARDANDO_APROVACAO = 2, 
    EM_EXECUCAO = 3, 
    FINALIZADA = 4, 
    ENTREGUE = 5, 
    APROVADA = 6, 
    CANCELADA = 7
}
