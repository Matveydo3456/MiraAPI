﻿using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities;
using Reactor.Utilities;
using Reactor.Utilities.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace MiraAPI.Modifiers;

/// <summary>
/// The component for handling modifiers.
/// </summary>
[RegisterInIl2Cpp]
public class ModifierComponent(IntPtr cppPtr) : MonoBehaviour(cppPtr)
{
    /// <summary>
    /// Gets the active modifiers on the player.
    /// </summary>
    [HideFromIl2Cpp]
    public ImmutableList<BaseModifier> ActiveModifiers => Modifiers.ToImmutableList();

    [HideFromIl2Cpp]
    private List<BaseModifier> Modifiers { get; set; } = [];

    private PlayerControl? _player;

    private TextMeshPro? _modifierText;

    private readonly List<BaseModifier> _toRemove = [];

    private readonly List<BaseModifier> _toAdd = [];

    internal void ClearModifiers()
    {
        _toRemove.AddRange(Modifiers);
    }

    private void Start()
    {
        _player = GetComponent<PlayerControl>();
        Modifiers = [];

        if (!_player.AmOwner)
        {
            return;
        }

        _modifierText = Helpers.CreateTextLabel("ModifierText", HudManager.Instance.transform, AspectPosition.EdgeAlignments.RightTop, new Vector3(10.1f, 3.5f, 0), textAlignment: TextAlignmentOptions.Right);
        _modifierText.verticalAlignment = VerticalAlignmentOptions.Top;
    }

    private void FixedUpdate()
    {
        foreach (var modifier in _toRemove)
        {
            modifier.OnDeactivate();
            Modifiers.Remove(modifier);
        }

        foreach (var modifier in _toAdd)
        {
            Modifiers.Add(modifier);
            modifier.Initialized = true;
            modifier.OnActivate();

            if (modifier is TimedModifier { AutoStart: true } timer)
            {
                timer.StartTimer();
            }
        }

        if (_toAdd.Count > 0 || _toRemove.Count > 0)
        {
            if (_player?.AmOwner == true)
            {
                HudManager.Instance?.SetHudActive(_player, _player.Data.Role, HudManager.Instance.TaskPanel.isActiveAndEnabled);
            }
            _toAdd.Clear();
            _toRemove.Clear();
        }

        foreach (var modifier in Modifiers)
        {
            modifier.FixedUpdate();
        }
    }

    private void Update()
    {
        foreach (var modifier in Modifiers)
        {
            modifier.Update();
        }

        if (!_modifierText || _player?.AmOwner == false)
        {
            return;
        }

        var filteredModifiers = Modifiers.Where(mod => !mod.HideOnUi).ToArray();

        if (filteredModifiers.Length != 0 && !MeetingHud.Instance)
        {
            var stringBuild = new StringBuilder();
            foreach (var mod in filteredModifiers)
            {
                stringBuild.Append(CultureInfo.InvariantCulture, $"\n{mod.GetHudString()}");
            }
            _modifierText!.text = $"<b><size=130%>Modifiers:</b></size>{stringBuild}";
        }
        else if (_modifierText!.text != string.Empty)
        {
            _modifierText.text = string.Empty;
        }
    }

    /// <summary>
    /// Gets a collection of modifiers by type.
    /// </summary>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    /// <typeparam name="T">The Type of the Modifier.</typeparam>
    /// <returns>A collection of modifiers.</returns>
    [HideFromIl2Cpp]
    public IEnumerable<T> GetModifiers<T>(Func<T, bool>? predicate=null) where T : BaseModifier
    {
        return Modifiers.OfType<T>().Where(x => predicate == null || predicate(x));
    }

    /// <summary>
    /// Gets a collection of modifiers by type.
    /// </summary>
    /// <param name="type">The modifier type.</param>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    /// <returns>A collection of modifiers.</returns>
    [HideFromIl2Cpp]
    public IEnumerable<BaseModifier> GetModifiers(Type type, Func<BaseModifier, bool>? predicate=null)
    {
        return Modifiers.Where(x => x.GetType() == type && (predicate == null || predicate(x)));
    }

    /// <summary>
    /// Gets a collection of modifiers by type ID.
    /// </summary>
    /// <param name="id">The modifier's type ID.</param>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    /// <returns>A collection of modifiers.</returns>
    [HideFromIl2Cpp]
    public IEnumerable<BaseModifier> GetModifiers(uint id, Func<BaseModifier, bool>? predicate=null)
    {
        var type = ModifierManager.GetModifierType(id) ?? throw new InvalidOperationException(
            $"Cannot get modifier with id {id} because it is not registered.");

        return GetModifiers(type, predicate);
    }

    /// <summary>
    /// Tries to get a modifier by its type.
    /// </summary>
    /// <param name="modifier">The modifier or null.</param>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    /// <typeparam name="T">The Type of the Modifier.</typeparam>
    /// <returns>True if the modifier was found, false otherwise.</returns>
    [HideFromIl2Cpp]
    public bool TryGetModifier<T>(out T? modifier, Func<T, bool>? predicate = null) where T : BaseModifier
    {
        modifier = GetModifier(predicate);
        return modifier != null;
    }

    /// <summary>
    /// Tries to get a modifier by its type.
    /// </summary>
    /// <param name="type">The modifier type.</param>
    /// <param name="modifier">The modifier or null.</param>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    /// <returns>True if the modifier was found, false otherwise.</returns>
    [HideFromIl2Cpp]
    public bool TryGetModifier(Type type, out BaseModifier? modifier, Func<BaseModifier, bool>? predicate = null)
    {
        modifier = GetModifier(type, predicate);
        return modifier != null;
    }

    /// <summary>
    /// Tries to get a modifier by its type ID.
    /// </summary>
    /// <param name="id">The modifier type ID.</param>
    /// <param name="modifier">The modifier or null.</param>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    /// <returns>True if the modifier was found, false otherwise.</returns>
    [HideFromIl2Cpp]
    public bool TryGetModifier(uint id, out BaseModifier? modifier, Func<BaseModifier, bool>? predicate = null)
    {
        modifier = GetModifier(id, predicate);
        return modifier != null;
    }

    /// <summary>
    /// Tries to get a modifier by its unique ID.
    /// </summary>
    /// <param name="modifierGuid">The modifier unique ID.</param>
    /// <param name="modifier">The modifier or null.</param>
    /// <returns>True if the modifier was found, false otherwise.</returns>
    [HideFromIl2Cpp]
    public bool TryGetModifier(Guid modifierGuid, out BaseModifier? modifier)
    {
        modifier = GetModifier(modifierGuid);
        return modifier != null;
    }

    /// <summary>
    /// Gets a modifier by its type.
    /// </summary>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    /// <typeparam name="T">The Type of the Modifier.</typeparam>
    /// <returns>The Modifier if it is found, null otherwise.</returns>
    [HideFromIl2Cpp]
    public T? GetModifier<T>(Func<T, bool>? predicate = null) where T : BaseModifier
    {
        return GetModifiers(predicate).FirstOrDefault();
    }

    /// <summary>
    /// Gets a modifier by its type.
    /// </summary>
    /// <param name="type">The modifier type.</param>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    /// <returns>The Modifier if it is found, null otherwise.</returns>
    [HideFromIl2Cpp]
    public BaseModifier? GetModifier(Type type, Func<BaseModifier, bool>? predicate = null)
    {
        return GetModifiers(type).FirstOrDefault(predicate ?? (_ => true));
    }

    /// <summary>
    /// Gets a modifier by its type ID.
    /// </summary>
    /// <param name="id">The modifier ID.</param>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    /// <returns>The Modifier if it is found, null otherwise.</returns>
    [HideFromIl2Cpp]
    public BaseModifier? GetModifier(uint id, Func<BaseModifier, bool>? predicate = null)
    {
        var type = ModifierManager.GetModifierType(id) ?? throw new InvalidOperationException(
            $"Cannot get modifier with id {id} because it is not registered.");

        return GetModifier(type, predicate);
    }

    /// <summary>
    /// Gets a modifier by unique ID.
    /// </summary>
    /// <param name="modifierGuid">The modifier's unique ID.</param>
    /// <returns>The modifier if it is found, or null.</returns>
    [HideFromIl2Cpp]
    public BaseModifier? GetModifier(Guid modifierGuid)
    {
        return Modifiers.Find(x => x.UniqueId == modifierGuid);
    }

    /// <summary>
    /// Removes a modifier from the player.
    /// </summary>
    /// <typeparam name="T">The modifier type.</typeparam>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    [HideFromIl2Cpp]
    public void RemoveModifier<T>(Func<T, bool>? predicate = null) where T : BaseModifier
    {
        RemoveModifier(typeof(T), x => predicate == null || predicate((T)x));
    }

    /// <summary>
    /// Removes a modifier from the player.
    /// </summary>
    /// <param name="type">The modifier type.</param>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    [HideFromIl2Cpp]
    public void RemoveModifier(Type type, Func<BaseModifier, bool>? predicate = null)
    {
        var modifiers = Modifiers.Where(x => x.GetType() == type && (predicate == null || predicate(x))).ToList();
        if (modifiers.Count > 1)
        {
            throw new InvalidOperationException($"Cannot remove modifier {type.Name} because there are multiple instances of that modifier.");
        }

        var modifier = modifiers.FirstOrDefault();
        if (modifier is null)
        {
            Logger<MiraApiPlugin>.Error($"Cannot remove modifier {type.Name} because it is not active.");
            return;
        }

        RemoveModifier(modifier);
    }

    /// <summary>
    /// Removes a modifier from the player.
    /// </summary>
    /// <param name="modifier">The modifier object.</param>
    [HideFromIl2Cpp]
    public void RemoveModifier(BaseModifier modifier)
    {
        if (!Modifiers.Contains(modifier))
        {
            Logger<MiraApiPlugin>.Error($"Cannot remove modifier {modifier.ModifierName} because it is not active on this player.");
            return;
        }

        _toRemove.Add(modifier);
    }

    /// <summary>
    /// Removes a modifier from the player.
    /// </summary>
    /// <param name="typeId">The modifier's type ID.</param>
    /// <param name="predicate">The predicate to check the modifier by.</param>
    [HideFromIl2Cpp]
    public void RemoveModifier(uint typeId, Func<BaseModifier, bool>? predicate = null)
    {
        var type = ModifierManager.GetModifierType(typeId) ?? throw new InvalidOperationException(
            $"Cannot remove modifier with id {typeId} because it is not registered.");
        RemoveModifier(type, predicate);
    }

    /// <summary>
    /// Removes a modifier from the player.
    /// </summary>
    /// <param name="uniqueId">The modifier's unique ID.</param>
    [HideFromIl2Cpp]
    public void RemoveModifier(Guid uniqueId)
    {
        var modifier = GetModifier(uniqueId);
        if (modifier == null)
        {
            Logger<MiraApiPlugin>.Error($"Cannot remove modifier with unique id {uniqueId} because it is not active.");
            return;
        }

        RemoveModifier(modifier);
    }

    /// <summary>
    /// Adds a modifier to the player.
    /// </summary>
    /// <param name="args">The arguments to initialize the modifier constructor with.</param>
    /// <typeparam name="T">The Type of the modifier.</typeparam>
    /// <returns>The new modifier.</returns>
    [HideFromIl2Cpp]
    public T? AddModifier<T>(params object[] args) where T : BaseModifier
    {
        return AddModifier(typeof(T), args) as T;
    }

    /// <summary>
    /// Adds a modifier to the player.
    /// </summary>
    /// <param name="modifier">The modifier to add.</param>
    /// <returns>The modifier that was added.</returns>
    [HideFromIl2Cpp]
    public BaseModifier? AddModifier(BaseModifier modifier)
    {
        // TODO: Make a proper synchronization system.
        if (LobbyBehaviour.Instance)
        {
            Logger<MiraApiPlugin>.Warning($"Modifiers added in the lobby won't sync to new players!");
        }

        var id = modifier.TypeId;
        if (modifier.Unique && Modifiers.Find(x => x.TypeId == id) != null)
        {
            Logger<MiraApiPlugin>.Error($"Player already has modifier with id {id}!");
            return null;
        }

        if (Modifiers.Contains(modifier))
        {
            Logger<MiraApiPlugin>.Error($"Player already has this modifier!");
            return null;
        }

        _toAdd.Add(modifier);
        modifier.Player = _player;
        modifier.ModifierComponent = this;
        if (modifier.UniqueId == Guid.Empty)
        {
            modifier.UniqueId = Guid.NewGuid();
        }
        return modifier;
    }

    /// <summary>
    /// Adds a modifier to the player.
    /// </summary>
    /// <param name="type">The modifier type.</param>
    /// <param name="args">The arguments to initialize the modifier constructor with.</param>
    /// <returns>The modifier that was added.</returns>
    [HideFromIl2Cpp]
    public BaseModifier? AddModifier(Type type, params object[] args)
    {
        BaseModifier? modifier;
        if (args.Length > 0)
        {
            modifier = ModifierFactory.CreateInstance(type, args);
        }
        else
        {
            modifier = Activator.CreateInstance(type) as BaseModifier;
            if (modifier == null)
            {
                throw new InvalidOperationException($"Cannot add modifier {type.Name} because it is not a valid modifier.");
            }
        }

        return AddModifier(modifier);
    }

    /// <summary>
    /// Adds a modifier to the player.
    /// </summary>
    /// <param name="id">The ID of the modifier.</param>
    /// <param name="args">The arguments to initialize the modifier constructor with.</param>
    /// <returns>The modifier if it was created, or null if it failed.</returns>
    [HideFromIl2Cpp]
    public BaseModifier? AddModifier(uint id, params object[] args)
    {
        var type = ModifierManager.GetModifierType(id) ?? throw new InvalidOperationException(
            $"Cannot add modifier with id {id} because it is not registered.");

        return AddModifier(type, args);
    }

    /// <summary>
    /// Checks if a player has an active modifier by its type.
    /// </summary>
    /// <param name="predicate">The predicate to check the modifier.</param>
    /// <typeparam name="T">The Type of the Modifier.</typeparam>
    /// <returns>True if the Modifier is present, false otherwise.</returns>
    [HideFromIl2Cpp]
    public bool HasModifier<T>(Func<T, bool>? predicate=null) where T : BaseModifier
    {
        return Modifiers.Exists(x => x is T modifier && (predicate == null || predicate(modifier)));
    }

    /// <summary>
    /// Checks if a player has an active modifier by its type.
    /// </summary>
    /// <param name="type">The modifier type.</param>
    /// <param name="predicate">The predicate to check the modifier.</param>
    /// <returns>True if the Modifier is present, false otherwise.</returns>
    [HideFromIl2Cpp]
    public bool HasModifier(Type type, Func<BaseModifier, bool>? predicate=null)
    {
        return Modifiers.Exists(x => x.GetType() == type && (predicate == null || predicate(x)));
    }

    /// <summary>
    /// Checks if a player has an active modifier by its type ID.
    /// </summary>
    /// <param name="id">The modifier's type ID.</param>
    /// <param name="predicate">The predicate to check the modifier.</param>
    /// <returns>True if the modifier is present, false otherwise.</returns>
    [HideFromIl2Cpp]
    public bool HasModifier(uint id, Func<BaseModifier, bool>? predicate=null)
    {
        var type = ModifierManager.GetModifierType(id) ?? throw new InvalidOperationException(
            $"Cannot get modifier with id {id} because it is not registered.");

        return HasModifier(type, predicate);
    }

    /// <summary>
    /// Checks if a player has an active modifier by its unique ID.
    /// </summary>
    /// <param name="id">The modifier's guid.</param>
    /// <returns>True if the modifier is present, false otherwise.</returns>
    [HideFromIl2Cpp]
    public bool HasModifier(Guid id)
    {
        return Modifiers.Exists(x => x.UniqueId == id);
    }
}
