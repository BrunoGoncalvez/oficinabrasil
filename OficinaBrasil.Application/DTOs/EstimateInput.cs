namespace OficinaBrasil.Application;

public class EstimateInput
{
    public string Diagnosis { get; set; } = "";
    public List<OrderServiceInput> Services { get; set; } = new();
    public List<OrderPartInput> Parts { get; set; } = new();
}
