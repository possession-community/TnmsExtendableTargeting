using System;
using System.Globalization;
using Sharp.Shared.Objects;
using TnmsExtendableTargeting.Shared;

namespace TnmsExtendableTargeting.BuiltinTargets;

public class Me: ICustomTargetCaller
{
    public string Prefix => "@me";
    
    public string? LocalizedTargetName(CultureInfo culture) 
        => null;

    public IGameClient? Resolve(IGameClient? caller)
        => caller;
}