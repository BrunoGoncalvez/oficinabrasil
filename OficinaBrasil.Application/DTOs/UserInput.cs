using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class UserInput
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public EmployeeRole Role { get; set; }

    public UserInput()
    {
    }

    public UserInput(string name, string email, string password, EmployeeRole role)
    {
        Name = name;
        Email = email;
        Password = password;
        Role = role;
    }
}
