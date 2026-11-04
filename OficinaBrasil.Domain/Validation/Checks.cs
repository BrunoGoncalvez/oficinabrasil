using System.Text.RegularExpressions;
namespace OficinaBrasil.Domain;

public static class Checks
{
    public static string Document(string value)
    {
        if (!Regex.IsMatch(value ?? "", @"^[0-9./\-\s]+$"))
            throw new BusinessException("Documento precisa conter apenas números e pontuação de CPF/CNPJ");
        var v = Regex.Replace(value ?? "", "[^0-9]", "");
        if (v.Length == 11)
        {
            if (!ValidCpf(v))
                throw new BusinessException("CPF inválido");
        }
        else if (v.Length == 14)
        {
            if (!ValidCnpj(v))
                throw new BusinessException("CNPJ inválido");
        }
        else throw new BusinessException("Documento precisa ser CPF ou CNPJ válido");
        return v;
    }
    static bool ValidCpf(string s)
    {
        if (s.Distinct().Count() == 1)
            return false;
        int d(int n)
        {
            int sum = 0;
            for (int i = 0; i<n; i++) sum +=(s[i] - '0') *(n + 1 - i);
            int rem =(sum * 10) % 11;
            return rem == 10? 0: rem;
        }
        return d(9) == s[9] - '0' && d(10) == s[10] - '0';
    }
    static bool ValidCnpj(string s)
    {
        if (s.Distinct().Count() == 1)
            return false;
        if (s.Distinct().Count() == 1)
            return false;
        int digit(int n, int[] weights)
        {
            int sum = 0;
            for (int i = 0; i<n; i++) sum +=(s[i] - '0') * weights[i];
            int rem = sum % 11;
            return rem< 2? 0: 11 - rem;
        }
        return digit(12, new[]
        {
            5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2
        }) == s[12] - '0' && digit(13, new[]
        {
            6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2
        }) == s[13] - '0';
    }

    public static string Plate(string value)
    {
        if (!Regex.IsMatch(value ?? "", @"^[A-Za-z0-9 \-]+$"))
            throw new BusinessException("Placa inválida");
        var s = Regex.Replace((value ?? "").ToUpperInvariant(), "[^A-Z0-9]", "");
        if (!Regex.IsMatch(s, @"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$"))
            throw new BusinessException("Placa inválida");
        return s;
    }

    public static void Required(string? v, string field)
    {
        if (string.IsNullOrWhiteSpace(v))
            throw new BusinessException($"{field} obrigatório");
    }
}
