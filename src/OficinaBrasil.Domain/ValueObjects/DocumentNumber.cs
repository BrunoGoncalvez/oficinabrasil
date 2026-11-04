namespace OficinaBrasil.Domain;

/// <summary>CPF/CNPJ numérico validado e normalizado; não representa autenticação.</summary>
public sealed record DocumentNumber
{
    public string Value { get; }
    public DocumentNumber(string value) => Value = Checks.Document(value);
    public override string ToString() => Value;
}
