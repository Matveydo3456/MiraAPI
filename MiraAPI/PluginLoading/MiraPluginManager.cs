﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Unity.IL2CPP;
using MiraAPI.Colors;
using MiraAPI.Cosmetics;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking;
using Reactor.Utilities;

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
        CustomCosmeticManager.RegisterVanilla();
        IL2CPPChainloader.Instance.PluginLoad += (pluginInfo, assembly, plugin) =>
        {
            if (plugin is not IMiraPlugin miraPlugin)
            {
                return;
            }

            var info = new MiraPluginInfo(miraPlugin, pluginInfo);

            RegisterModifierAttribute(assembly);
            RegisterAllOptions(assembly, info);
            RegisterAllCosmetics(assembly, info);

            RegisterRoleAttribute(assembly, info);
            RegisterButtonAttribute(assembly, info);

            RegisterColorClasses(assembly);

            _registeredPlugins.Add(assembly, info);

            Logger<MiraApiPlugin>.Info($"Registering mod {pluginInfo.Metadata.GUID} with Mira API.");
        };
        IL2CPPChainloader.Instance.Finished += PaletteManager.RegisterAllColors;
    }

    /// <summary>
    /// Get a mira plugin by its GUID.
    /// </summary>
    /// <param name="guid">The plugin GUID.</param>
    /// <returns>A MiraPluginInfo.</returns>
    public static MiraPluginInfo GetPluginByGuid(string guid)
    {
        return Instance._registeredPlugins.Values.First(plugin => plugin.PluginId == guid);
    }

    private static void RegisterAllOptions(Assembly assembly, MiraPluginInfo pluginInfo)
    {
        var filteredTypes = assembly.GetTypes().Where(type => type.IsAssignableTo(typeof(AbstractOptionGroup)));

        foreach (var type in filteredTypes)
        {
            if (!ModdedOptionsManager.RegisterGroup(type, pluginInfo))
            {
                continue;
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
        }

        pluginInfo.OptionGroups.Sort((x, y) => x.GroupPriority.CompareTo(y.GroupPriority));
    }

    private static void RegisterAllCosmetics(Assembly assembly, MiraPluginInfo pluginInfo)
    {
        var filteredTypes = assembly.GetTypes().Where(type => type.IsAssignableTo(typeof(AbstractCosmeticsGroup)));

        foreach (var type in filteredTypes)
        {
            if (!CustomCosmeticManager.RegisterGroup(type, pluginInfo))
            {
                continue;
            }

            foreach (var property in type.GetProperties())
            {
                var attribute = property.GetCustomAttribute<RegisterCustomCosmeticAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                CustomCosmeticManager.RegisterAttributeOption(type, attribute, property, pluginInfo);
            }
        }
    }

    private static void RegisterRoleAttribute(Assembly assembly, MiraPluginInfo pluginInfo)
    {
        List<Type> roles = [];
        foreach (var type in assembly.GetTypes())
        {
            var attribute = type.GetCustomAttribute<RegisterCustomRoleAttribute>();
            if (attribute == null)
            {
                continue;
            }

            if (!(typeof(RoleBehaviour).IsAssignableFrom(type) && typeof(ICustomRole).IsAssignableFrom(type)))
            {
                Logger<MiraApiPlugin>.Error($"{type.Name} does not inherit from RoleBehaviour or ICustomRole.");
                continue;
            }

            if (!ModList.GetById(pluginInfo.PluginId).IsRequiredOnAllClients)
            {
                Logger<MiraApiPlugin>.Error("Custom roles are only supported on all clients.");
                return;
            }

            roles.Add(type);
        }

        CustomRoleManager.RegisterRoleTypes(roles, pluginInfo);
    }

    private static void RegisterColorClasses(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<RegisterCustomColorsAttribute>() == null)
            {
                continue;
            }

            if (!type.IsStatic())
            {
                Logger<MiraApiPlugin>.Error($"Color class {type.Name} must be static.");
                continue;
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
    }

    private static void RegisterModifierAttribute(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            var attribute = type.GetCustomAttribute<RegisterModifierAttribute>();
            if (attribute != null)
            {
                ModifierManager.RegisterModifier(type);
            }
        }
    }

    private static void RegisterButtonAttribute(Assembly assembly, MiraPluginInfo pluginInfo)
    {
        foreach (var type in assembly.GetTypes())
        {
            var attribute = type.GetCustomAttribute<RegisterButtonAttribute>();
            if (attribute != null)
            {
                CustomButtonManager.RegisterButton(type, pluginInfo);
            }
        }
    }
}
