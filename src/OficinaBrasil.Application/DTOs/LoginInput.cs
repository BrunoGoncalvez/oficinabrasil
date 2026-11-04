using OficinaBrasil.Domain;
namespace OficinaBrasil.Application;

public class LoginInput
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public LoginInput()
    {
    }

    public LoginInput(string email, string password)
    {
        Email = email;
        Password = password;
    }
}
