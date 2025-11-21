using System;
using System.Globalization;
using Sharp.Shared.Objects;
using TnmsExtendableTargeting.Shared;

namespace TnmsExtendableTargeting.BuiltinTargets.SpecialImpl;

public class SharpPrefixed: ICustomTargetParameterized
{
    public string Prefix => "";
    
    public string? LocalizedTargetName(CultureInfo culture) 
        => null;

    public bool Resolve(string param, IGameClient targetClient, IGameClient? caller)
    {
        if (!uint.TryParse(param, out var result))
            return false;
        
        if (targetClient.UserId.AsPrimitive() == result)
            return true;

        if (targetClient.SteamId.AccountId == result)
            return true;
        
        return false;
    }
}