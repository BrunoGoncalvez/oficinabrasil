using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class CustomerInput
{
    public string Name { get; set; } = "";
    public string Document { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public CustomerInput()
    {
    }

    public CustomerInput(string name, string document, string? email, string? phone)
    {
        Name = name;
        Document = document;
        Email = email;
        Phone = phone;
    }
}
