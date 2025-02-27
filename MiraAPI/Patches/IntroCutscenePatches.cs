﻿using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using UnityEngine;

namespace MiraAPI.Patches;

[HarmonyPatch(typeof(IntroCutscene))]
public static class IntroCutscenePatches
{
    /*
    [HarmonyPostfix]
    [HarmonyPatch(nameof(IntroCutscene.BeginImpostor))]
    public static void BeginImpostorPatch(IntroCutscene __instance)
    {
        if (CustomGameModeManager.ActiveMode != null && CustomGameModeManager.ActiveMode.ShowCustomRoleScreen())
        {
            var mode = CustomGameModeManager.ActiveMode;
            __instance.TeamTitle.text = $"<size=70%>{mode.Name}</size>\n<size=20%>{mode.Description}</size>";
        }
    }*/

    [HarmonyPostfix]
    [HarmonyPatch(nameof(IntroCutscene.CoBegin))]
    public static void IntroBeginPatch(IntroCutscene __instance)
    {
        var @event = new IntroBeginEvent(__instance);
        MiraEventManager.InvokeEvent(@event);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(IntroCutscene.BeginImpostor))]
    [HarmonyPatch(nameof(IntroCutscene.BeginCrewmate))]
    public static bool BeginImpostorPatch(IntroCutscene __instance, [HarmonyArgument(0)] ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        if (PlayerControl.LocalPlayer.Data.Role is not ICustomRole customRole)
        {
            return true;
        }

        if (customRole.IntroConfiguration is { } introConfig)
        {
            __instance.BackgroundBar.material.SetColor(ShaderID.Color, introConfig.IntroTeamColor);
            __instance.TeamTitle.color = introConfig.IntroTeamColor;
            __instance.TeamTitle.text = introConfig.IntroTeamTitle;
            __instance.ImpostorText.text = introConfig.IntroTeamDescription;
        }

        return customRole.SetupIntroTeam(__instance, ref yourTeam);
    }

    /*
    [HarmonyPostfix]
    [HarmonyPatch(nameof(IntroCutscene.OnDestroy))]
    public static void GameBeginPatch()
    {
        CustomGameModeManager.ActiveMode?.Initialize();
    }*/
}
