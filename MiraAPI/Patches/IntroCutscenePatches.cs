﻿using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
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
    [HarmonyPatch(nameof(IntroCutscene.BeginCrewmate))]
    public static bool BeginCrewmatePatch(IntroCutscene __instance)
    {
        if (PlayerControl.LocalPlayer.Data.Role is not ICustomRole customRole)
        {
            return true;
        }

        if (customRole.Team is not ModdedRoleTeams.Neutral)
        {
            return true;
        }

        var barTransform = __instance.BackgroundBar.transform;
        var position = barTransform.position;
        position.y -= 0.25f;
        barTransform.position = position;

        __instance.BackgroundBar.material.SetColor(ShaderID.Color, Color.gray);
        __instance.TeamTitle.text = "NEUTRAL";
        __instance.impostorScale = 1f;
        __instance.ImpostorText.text = "You are Neutral. You do not have a team.";
        __instance.TeamTitle.color = Color.gray;

        __instance.ourCrewmate = __instance.CreatePlayer(
            0,
            Mathf.CeilToInt(7.5f),
            PlayerControl.LocalPlayer.Data,
            false);
        return false;
    }

    /*
    [HarmonyPostfix]
    [HarmonyPatch(nameof(IntroCutscene.OnDestroy))]
    public static void GameBeginPatch()
    {
        CustomGameModeManager.ActiveMode?.Initialize();
    }*/
}
