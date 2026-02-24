namespace TaleWeaver.Api.Data.Models;

/// <summary>
/// Key-value application configuration stored in the database.
/// </summary>
public class AppConfig : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Category { get; set; }
}
