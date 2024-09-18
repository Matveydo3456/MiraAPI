﻿using HarmonyLib;
using InnerNet;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;

namespace MiraAPI.Patches;

/// <summary>
/// Sync all options, role settings, and modifiers to the player when they join the game.
/// </summary>
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CreatePlayer))]
public static class AmongUsClientSyncPatch
{
    public static void Postfix(ClientData clientData)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (clientData.Id == AmongUsClient.Instance.HostId)
        {
            return;
        }

        ModdedOptionsManager.SyncAllOptions(clientData.Id);
        CustomRoleManager.SyncAllRoleSettings(clientData.Id);
        ModifierManager.SyncAllModifiers(clientData.Id);
    }
}
