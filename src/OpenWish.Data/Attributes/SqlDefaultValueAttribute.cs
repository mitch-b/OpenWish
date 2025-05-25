namespace OpenWish.Data.Attributes;

/// <summary>
/// Set a default value defined on the sql server
/// </summary>
/// <remarks>
/// Set a default value defined on the sql server
/// </remarks>
/// <param name="value">Default value to apply</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class SqlDefaultValueAttribute(string value) : Attribute
{
    /// <summary>
    /// Default value to apply
    /// </summary>
    public string DefaultValue { get; set; } = value;
}