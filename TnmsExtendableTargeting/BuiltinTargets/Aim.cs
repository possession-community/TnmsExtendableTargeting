using System;
using System.Globalization;
using Sharp.Shared;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using TnmsExtendableTargeting.Shared;

namespace TnmsExtendableTargeting.BuiltinTargets;

public class Aim(ISharedSystem sharedSystem): ICustomTargetCaller
{
    public string Prefix => "@aim";
    
    public string? LocalizedTargetName(CultureInfo culture) 
        => null;

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