using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class CustomerUseCases
{
    private readonly IWorkshopData data;

    public CustomerUseCases(IWorkshopData data)
    {
        this.data = data;
    }

    public Task<Customer?> FindByDocument(string document)
    {
        var normalized = new DocumentNumber(document).Value;
        return data.FindCustomerByDocument(normalized);
    }

    public async Task<Customer> CreateCustomer(CustomerInput input)
    {
        Checks.Required(input.Name, "Nome");
        var doc = new DocumentNumber(input.Document).Value;
        if (await data.FindCustomerByDocument(doc) != null)
            throw new BusinessException("CPF/CNPJ já cadastrado");
        var c = new Customer
        {
            Name = input.Name.Trim(), Document = doc, Email = input.Email, Phone = input.Phone
        };
        await data.Customers.Add(c);
        await data.Save();
        return c;
    }
}
