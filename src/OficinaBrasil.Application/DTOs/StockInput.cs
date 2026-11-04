using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class StockInput
{
    public int Delta { get; set; }
    public string Reason { get; set; } = "";
    public StockInput()
    {
    }

    public StockInput(int delta, string reason)
    {
        Delta = delta;
        Reason = reason;
    }
}
