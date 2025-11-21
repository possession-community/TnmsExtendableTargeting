using System.Globalization;

namespace TnmsExtendableTargeting.Shared;

public interface ICustomTargetBase
{
    string Prefix { get; }
    
    string? LocalizedTargetName(CultureInfo culture);
}