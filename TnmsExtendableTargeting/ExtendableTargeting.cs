using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Objects;
using TnmsExtendableTargeting.BuiltinTargets;
using TnmsExtendableTargeting.BuiltinTargets.SpecialImpl;
using TnmsExtendableTargeting.Shared;

namespace TnmsExtendableTargeting;

public class ExtendableTargeting: IModSharpModule, IExtendableTargeting
{
    private readonly ILogger _logger;
    private readonly ISharedSystem _sharedSystem;

    private readonly ICustomTargetParameterized _nameContainedPlayer = new NameContainedPlayer();
    private readonly ICustomTargetParameterized _sharpPrefixed = new SharpPrefixed();
    
    public ExtendableTargeting(ISharedSystem sharedSystem,
        string?                  dllPath,
        string?                  sharpPath,
        Version?                 version,
        IConfiguration?          coreConfiguration,
        bool                     hotReload)
    {
        _sharedSystem = sharedSystem;
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<ExtendableTargeting>();
        
        ArgumentNullException.ThrowIfNull(dllPath);
        ArgumentNullException.ThrowIfNull(sharpPath);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(coreConfiguration);
    }
    
    public string DisplayName => "TnmsExtendableTargeting";
    public string DisplayAuthor => "faketuna";
    
    public bool Init()
    {
        _logger.LogInformation("TnmsExtendableTargeting initialized");
        
        RegisterBuiltinTargets();
        return true;
    }

    public void PostInit()
    {
        _sharedSystem.GetSharpModuleManager().RegisterSharpModuleInterface(this, IExtendableTargeting.ModSharpModuleIdentity, (IExtendableTargeting)this);
    }

    public void Shutdown()
    {
        _logger.LogInformation("TnmsExtendableTargeting shutdown");
    }

    private void RegisterBuiltinTargets()
    {
        RegisterCustomTarget(new All());
        RegisterCustomSingleTarget(new Me());
        RegisterCustomTarget(new WithOutMe());
        RegisterCustomSingleTarget(new Aim(_sharedSystem));
        RegisterCustomTarget(new Ct());
        RegisterCustomTarget(new Te());
        RegisterCustomTarget(new Spectator());
        RegisterCustomTarget(new Bot());
        RegisterCustomTarget(new Human());
        RegisterCustomTarget(new Alive());
        RegisterCustomTarget(new Dead());
    }
     
     // Custom target
     private readonly Dictionary<string, ICustomTarget> _customTargets = new(StringComparer.OrdinalIgnoreCase);
     
     // Custom single target
     private readonly Dictionary<string, ICustomTargetCaller> _singleTargets = new(StringComparer.OrdinalIgnoreCase);
         
     // Parameterized target
     private readonly Dictionary<string, ICustomTargetParameterized> _paramTargets = new(StringComparer.OrdinalIgnoreCase);


     public void RegisterCustomSingleTarget(ICustomTargetCaller predicate)
     {
         string prefix = NormalizePrefix(predicate.Prefix);
         
         if (CheckPrefixIsRegistered(prefix))
                throw new ArgumentException("The specified prefix is already registered. please consider using another prefix");

         _singleTargets.Add(prefix, predicate);
     }

     public bool UnregisterCustomSingleTarget(string prefix)
     {
         return _singleTargets.Remove(prefix);
     }

     public void RegisterCustomTarget(ICustomTarget predicate)
     {
         string prefix = NormalizePrefix(predicate.Prefix);

         
         if (CheckPrefixIsRegistered(prefix))
             throw new ArgumentException("The specified prefix is already registered. please consider using another prefix");

         _customTargets.TryAdd(prefix, predicate);
     }

     public bool UnregisterCustomTarget(string prefix)
     {
         return _customTargets.Remove(prefix);
     }

     public void RegisterCustomParameterizedTarget(ICustomTargetParameterized predicate)
     {
         string prefix = NormalizePrefix(predicate.Prefix);
         
         if (CheckPrefixIsRegistered(prefix))
             throw new ArgumentException("The specified prefix is already registered. please consider using another prefix");

         _paramTargets.TryAdd(prefix, predicate);
     }

     public bool UnregisterCustomParameterizedTarget(string prefix)
     {
         return _paramTargets.Remove(prefix);
     }

     public bool ResolveTarget(string targetString, IGameClient? caller, out ITargetingResult? targetingResult)
     {
         targetingResult = null;
         
         if (targetString.StartsWith('#') && targetString.Length > 1)
         {
             var param = targetString.Substring(1);
             
             List<IGameClient> players = new();
             foreach (var client in _sharedSystem.GetModSharp().GetIServer().GetGameClients(true, false))
             {
                 if (_sharpPrefixed.Resolve(param, client, caller))
                     players.Add(client);
             }

             if (players.Count > 0)
             {
                 targetingResult = new TargetingResult(players, _sharpPrefixed);
                 return true;
             }
             
             return false;
         }
         
         if (_singleTargets.TryGetValue(targetString, out var singlePredicate))
         {
             if (singlePredicate.Resolve(caller) is {} target)
             {
                 targetingResult = new TargetingResult([target], singlePredicate);
                 return true;
             }
             
             return false;
         }
         
         if (_customTargets.TryGetValue(targetString, out var predicate))
         {
             List<IGameClient> players = new();
             foreach (var client in _sharedSystem.GetModSharp().GetIServer().GetGameClients(true, false))
             {
                 if (predicate.Resolve(client, caller))
                     players.Add(client);
             }

             if (players.Count > 0)
             {
                 targetingResult = new TargetingResult(players, predicate);
                 return true;
             }

             return false;
         }

         if (targetString.StartsWith('@') && targetString.Contains('='))
         {
             var parts = targetString.Split('=', 2);
             var prefix = parts[0];
             var param = parts[1];
             
             if (param.Length < 1)
                 return false;

             if (_paramTargets.TryGetValue(prefix, out var paramPredicate))
             {
                 List<IGameClient> players = new();
                 foreach (var client in _sharedSystem.GetModSharp().GetIServer().GetGameClients(true, false))
                 {
                     if (paramPredicate.Resolve(param, client, caller))
                         players.Add(client);
                 }

                 if (players.Count > 0)
                 {
                     targetingResult = new TargetingResult(players, paramPredicate);
                     return true;
                 }
                 
                 return false;
             }
         }
         
         List<IGameClient> nameContainedPlayers = new();
         foreach (var client in _sharedSystem.GetModSharp().GetIServer().GetGameClients(true, false))
         {
             if (_nameContainedPlayer.Resolve(targetString, client, caller))
                 nameContainedPlayers.Add(client);
         }

         if (nameContainedPlayers.Count > 0)
         {
             targetingResult = new TargetingResult(nameContainedPlayers, _nameContainedPlayer);
             return true;
         }
         
         return false;
     }
     
     
     private bool CheckPrefixIsRegistered(string prefix)
     {
         if (!prefix.StartsWith('@'))
         {
             prefix = '@' + prefix;
         }

         return _customTargets.ContainsKey(prefix) || _singleTargets.ContainsKey(prefix) || _paramTargets.ContainsKey(prefix);
     }
     
     private string NormalizePrefix(string prefix)
     {
         return prefix.StartsWith('@') ? prefix : '@' + prefix;
     }
}