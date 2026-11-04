namespace OficinaBrasil.Domain;

public sealed record LicensePlate
{
    public string Value { get; }
    public LicensePlate(string value) => Value = Checks.Plate(value);
    public override string ToString() => Value;
}
