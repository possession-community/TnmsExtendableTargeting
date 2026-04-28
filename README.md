# [TnmsExtendableTargeting](https://github.com/possession-community/TnmsExtendableTargeting)

## What is this?

This module provides extendable targeting functionality.

It offers the following types of targeting:

- `@prefix` - Target without parameters
- `@prefix=value` - Target with parameters

## Built-in Targets

The following targeting options are provided out of the box:

| Target String | Description                           | Example    |
|---------------|---------------------------------------|------------|
| `@me`         | Targets yourself.                     | `@me`      |
| `@!me`        | Targets everyone except yourself.     | `@!me`     |
| `@aim`        | Targets the player you are aiming at. | `@aim`     |
| `@ct`         | Targets players on the CT team.       | `@ct`      |
| `@t`          | Targets players on the T team.        | `@t`       |
| `@spec`       | Targets spectators.                   | `@spec`    |
| `@bot`        | Targets bot players.                  | `@bot`     |
| `@human`      | Targets human players.                | `@human`   |
| `@alive`      | Targets alive players.                | `@alive`   |
| `@dead`       | Targets dead players.                 | `@dead`    |
| `#<number>`   | Targets by SteamID or UserID.         | `#12345`   |

## Usage

### Dependencies

Install `TnmsExtendableTargeting.Shared` from NuGet:

```shell
dotnet add package TnmsExtendableTargeting.Shared
```

### Example

You can obtain `IExtendableTargeting` as follows:

```csharp
private IExtendableTargeting _extendableTargeting = null!;

public void OnAllModulesLoaded()
{
    var extendableTargeting = _sharedSystem.GetSharpModuleManager()
        .GetRequiredSharpModuleInterface<IExtendableTargeting>(IExtendableTargeting.ModSharpModuleIdentity).Instance;
    _extendableTargeting = extendableTargeting ?? throw new InvalidOperationException("TnmsExtendableTargeting is not found! Make sure TnmsExtendableTargeting is installed!");
}
```

Then, resolve targets as follows:

```csharp
if(_extendableTargeting.ResolveTarget(targetString, player, out var foundTargets))
{
    // Do something with foundTargets
}
```

## Creating Custom Targets

As the name suggests, ExtendableTargeting allows you to add and extend custom targets.

Once added, custom targets can be used anywhere `IExtendableTargeting.ResolveTarget` is used. In other words, they are available in all modules that use this functionality.

### Custom Single Target (Returns a single player)

Register a custom single target using `IExtendableTargeting.RegisterCustomSingleTarget`.

Create a class that implements `ICustomTargetCaller`:

```csharp
using System;
using System.Globalization;
using Sharp.Shared;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using TnmsExtendableTargeting.Shared;

public class Aim(ISharedSystem sharedSystem): ICustomTargetCaller
{
    public string Prefix => "@aim";

    public string LocalizedTargetName(CultureInfo culture)
        => throw new InvalidOperationException("This target does not support translation.");

    public IGameClient? Resolve(IGameClient? caller)
    {
        if (caller == null)
            return null;

        var callerPawn = caller.GetPlayerController()?.GetPlayerPawn();

        if (callerPawn == null)
            return null;

        var startPos = callerPawn.GetEyePosition();
        var eyeAngle = callerPawn.GetEyeAngles();
        var endPos = startPos + (eyeAngle.AnglesToVectorForward() * 2048);

        var trace = sharedSystem.GetPhysicsQueryManager()
            .TraceLine(startPos,
                endPos,
                UsefulInteractionLayers.FireBullets,
                CollisionGroupType.Default,
                TraceQueryFlag.All,
                InteractionLayers.None,
                callerPawn);

        if (trace.HitEntity?.AsPlayerPawn() is not {LifeState: LifeState.Alive} hitPawn)
            return null;

        var hitController = hitPawn.GetController();

        if (hitController == null)
            return null;

        return sharedSystem.GetClientManager().GetGameClient(hitController.PlayerSlot);
    }
}
```

Then register it:

```csharp
_extendableTargeting.RegisterCustomSingleTarget(new Aim(_sharedSystem));
```

### Custom Target (Returns multiple players)

Register a custom target using `IExtendableTargeting.RegisterCustomTarget`.

Create a class that implements `ICustomTarget`:

```csharp
using System.Globalization;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using TnmsExtendableTargeting.Shared;

public class CustomTeamTarget : ICustomTarget
{
    public string Prefix => "@ct";

    public string LocalizedTargetName(CultureInfo culture)
    {
        return "Counter Terrorists";
    }

    public bool Resolve(IGameClient targetClient, IGameClient? caller)
    {
        return targetClient.GetPlayerController()?.GetPlayerPawn()?.Team == CStrikeTeam.CT;
    }
}
```

Then register it:

```csharp
_extendableTargeting.RegisterCustomTarget(new CustomTeamTarget());
```

### Custom Parameterized Target (With parameters)

Register a custom target with parameters using `IExtendableTargeting.RegisterCustomParameterizedTarget`.

Create a class that implements `ICustomTargetParameterized`:

```csharp
using System;
using System.Globalization;
using Sharp.Shared.Objects;
using TnmsExtendableTargeting.Shared;

public class SlotTarget : ICustomTargetParameterized
{
    public string Prefix => "@slot";

    public string LocalizedTargetName(CultureInfo culture)
        => throw new InvalidOperationException("This target does not support translation.");

    public bool Resolve(string param, IGameClient targetClient, IGameClient? caller)
    {
        if (int.TryParse(param, out var slot))
        {
            return targetClient.Slot.AsPrimitive() == slot;
        }
        return false;
    }
}
```

Then register it:

```csharp
_extendableTargeting.RegisterCustomParameterizedTarget(new SlotTarget());
```

Custom targets with parameters can be used in the form `@prefix=value`, for example: `@slot=5`.
