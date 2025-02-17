﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using MiraAPI.Colors;
using MiraAPI.Events;
using MiraAPI.GameModes;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking;
using Reactor.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace MiraAPI.PluginLoading;

/// <summary>
/// Mira Plugin manager.
/// </summary>
public sealed class MiraPluginManager
{
    private readonly Dictionary<Assembly, MiraPluginInfo> _registeredPlugins = [];

    internal MiraPluginInfo[] RegisteredPlugins() => [.. _registeredPlugins.Values];

    internal static MiraPluginManager Instance { get; private set; } = new();

    internal void Initialize()
    {
        Instance = this;
        RegisterPlugin(
            IL2CPPChainloader.Instance.Plugins[MiraApiPlugin.Id],
            typeof(MiraApiPlugin).Assembly,
            MiraApiPlugin.Instance);
        CustomGameModeManager.RegisterDefaultMode();
        CustomGameModeManager.GetAndSetGameMode();

        IL2CPPChainloader.Instance.PluginLoad += RegisterPlugin;
        IL2CPPChainloader.Instance.Finished += PaletteManager.RegisterAllColors;
        IL2CPPChainloader.Instance.Finished += MiraEventManager.SortAllHandlers;
        IL2CPPChainloader.Instance.Finished += () =>
        {
            CustomButtonManager.Buttons = new ReadOnlyCollection<CustomActionButton>(CustomButtonManager.CustomButtons);
        };
    }

    private void RegisterPlugin(PluginInfo pluginInfo, Assembly assembly, BasePlugin plugin)
    {
        if (plugin is not IMiraPlugin miraPlugin || _registeredPlugins.ContainsKey(assembly))
        {
            return;
        }

        var info = new MiraPluginInfo(miraPlugin, pluginInfo);
        var roles = new List<Type>();

        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<MiraIgnoreAttribute>() != null)
            {
                continue;
            }

            if (RegisterModifier(type))
            {
                continue;
            }

            if (RegisterOptions(type, info))
            {
                continue;
            }

            if (RegisterRoleAttribute(type, info, out var role))
            {
                roles.Add(role!);
                continue;
            }

            if (RegisterButtonAttribute(type, info))
            {
                continue;
            }

            if (RegisterGameModeAttribute(type, info))
            {
                continue;
            }

            RegisterColorClasses(type);
        }

        info.OptionGroups.Sort((x, y) => x.GroupPriority.CompareTo(y.GroupPriority));
        CustomRoleManager.RegisterRoleTypes(roles, info);

        _registeredPlugins.Add(assembly, info);
        Logger<MiraApiPlugin>.Info($"Registering mod {pluginInfo.Metadata.GUID} with Mira API.");
    }

    /// <summary>
    /// Get a mira plugin by its GUID.
    /// </summary>
    /// <param name="pluginId">The plugin GUID.</param>
    /// <returns>A MiraPluginInfo.</returns>
    public static MiraPluginInfo GetPluginByGuid(string pluginId)
    {
        return Instance._registeredPlugins.Values.First(plugin => plugin.PluginId == pluginId);
    }

    private static bool RegisterOptions(Type type, MiraPluginInfo pluginInfo)
    {
        try
        {
            if (!type.IsAssignableTo(typeof(AbstractOptionGroup)))
            {
                return false;
            }

            if (!ModdedOptionsManager.RegisterGroup(type, pluginInfo))
            {
                return false;
            }

            foreach (var property in type.GetProperties())
            {
                if (property.PropertyType.IsAssignableTo(typeof(IModdedOption)))
                {
                    ModdedOptionsManager.RegisterPropertyOption(type, property, pluginInfo);
                    continue;
                }

                var attribute = property.GetCustomAttribute<ModdedOptionAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                ModdedOptionsManager.RegisterAttributeOption(type, attribute, property, pluginInfo);
            }

            return true;
        }
        catch (Exception e)
        {
            Logger<MiraApiPlugin>.Error($"Failed to register options for {type.Name}: {e}");
        }
        return false;
    }

    private static bool RegisterRoleAttribute(Type type, MiraPluginInfo pluginInfo, out Type? role)
    {
        role = null;
        try
        {
            if (!(typeof(RoleBehaviour).IsAssignableFrom(type) && typeof(ICustomRole).IsAssignableFrom(type)))
            {
                return false;
            }

            if (!ModList.GetById(pluginInfo.PluginId).IsRequiredOnAllClients)
            {
                Logger<MiraApiPlugin>.Error("Custom roles are only supported on all clients.");
                return false;
            }

            role = type;
            return true;
        }
        catch (Exception e)
        {
            Logger<MiraApiPlugin>.Error($"Failed to register role for {type.Name}: {e}");
        }
        return false;
    }

    private static void RegisterColorClasses(Type type)
    {
        try
        {
            if (type.GetCustomAttribute<RegisterCustomColorsAttribute>() == null)
            {
                return;
            }

            if (!type.IsStatic())
            {
                Logger<MiraApiPlugin>.Error($"Color class {type.Name} must be static.");
                return;
            }

            foreach (var property in type.GetProperties())
            {
                if (property.PropertyType != typeof(CustomColor))
                {
                    continue;
                }

                if (property.GetValue(null) is not CustomColor color)
                {
                    Logger<MiraApiPlugin>.Error($"Color property {property.Name} in {type.Name} is not a CustomColor.");
                    continue;
                }

                PaletteManager.CustomColors.Add(color);
            }
        }
        catch (Exception e)
        {
            Logger<MiraApiPlugin>.Error($"Failed to register color class {type.Name}: {e}");
        }
    }

    private static bool RegisterModifier(Type type)
    {
        try
        {
            return ModifierManager.RegisterModifier(type);
        }
        catch (Exception e)
        {
            Logger<MiraApiPlugin>.Error($"Failed to register modifier {type.Name}: {e}");
            return false;
        }
    }

    private static bool RegisterButtonAttribute(Type type, MiraPluginInfo pluginInfo)
    {
        try
        {
            return CustomButtonManager.RegisterButton(type, pluginInfo);
        }
        catch (Exception e)
        {
            Logger<MiraApiPlugin>.Error($"Failed to register button {type.Name}: {e}");
        }

        return false;
    }

    private static bool RegisterGameModeAttribute(Type type, MiraPluginInfo pluginInfo)
    {
        try
        {
            if (type.IsAssignableTo(typeof(AbstractGameMode)))
            {
                // TODO: bool
                CustomGameModeManager.RegisterGameMode(type, pluginInfo);
                return true;
            }
        }
        catch (Exception e)
        {
            Logger<MiraApiPlugin>.Error($"Failed to register gamemode {type.Name}: {e}");
        }

        return false;
    }
}
