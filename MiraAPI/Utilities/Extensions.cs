using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MiraAPI.Utilities;

/// <summary>
/// Extension methods for various classes.
/// </summary>
public static class Extensions
{
    internal static NetData GetNetData(this ICustomRole role)
    {
        var count = role.GetCount();
        var chance = role.GetChance();

        if (count == null)
        {
            Logger<MiraApiPlugin>.Error("Couldn't get role count for NetData, defaulting to zero.");
            count = 0;
        }

        if (chance == null)
        {
            Logger<MiraApiPlugin>.Error("Couldn't get role chance for NetData, defaulting to zero.");
            chance = 0;
        }

        return new NetData(
            RoleId.Get(role.GetType()),
            BitConverter.GetBytes(count.Value).AddRangeToArray(BitConverter.GetBytes(chance.Value)));
    }

    /// <summary>
    /// Enables stencil masking on a TMP text object.
    /// </summary>
    /// <param name="text">The TMP text.</param>
    public static void EnableStencilMasking(this TMP_Text text)
    {
        text.fontMaterial.SetFloat(ShaderID.Get("_Stencil"), 1);
        text.fontMaterial.SetFloat(ShaderID.Get("_StencilComp"), 4);
    }

    /// <summary>
    /// Checks if a type is static.
    /// </summary>
    /// <param name="type">The type being checked.</param>
    /// <returns>True if the type is static, false otherwise.</returns>
    public static bool IsStatic(this Type type)
    {
        return type is { IsClass: true, IsAbstract: true, IsSealed: true };
    }

    /// <summary>
    /// Gets a darkened version of a color.
    /// </summary>
    /// <param name="color">The original color.</param>
    /// <param name="darknessAmount">A darkness amount between 0 and 255.</param>
    /// <returns>The darkened color.</returns>
    public static Color32 GetShadowColor(this Color32 color, byte darknessAmount)
    {
        return
            new Color32(
                (byte)Mathf.Clamp(color.r - darknessAmount, 0, 255),
                (byte)Mathf.Clamp(color.g - darknessAmount, 0, 255),
                (byte)Mathf.Clamp(color.b - darknessAmount, 0, 255),
                byte.MaxValue);
    }

    /// <summary>
    /// Truncates a string to a specified length.
    /// </summary>
    /// <param name="value">The original string.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <param name="truncationSuffix">An option suffix to attach at the end of the truncated string.</param>
    /// <returns>A truncated string of maxLength with the attached suffix.</returns>
    public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "…")
    {
        return value?.Length > maxLength
            ? value[..maxLength] + truncationSuffix
            : value;
    }

    /// <summary>
    /// Chunks a collection of NetData into smaller arrays.
    /// </summary>
    /// <param name="dataCollection">A collection of NetData objects.</param>
    /// <param name="chunkSize">The max chunk size in bytes.</param>
    /// <returns>A Queue of NetData arrays.</returns>
    public static Queue<NetData[]> ChunkNetData(this IEnumerable<NetData> dataCollection, int chunkSize)
    {
        Queue<NetData[]> chunks = [];
        List<NetData> current = [];

        var count = 0;
        foreach (var netData in dataCollection)
        {
            var length = netData.GetLength();

            if (length > chunkSize)
            {
                Logger<MiraApiPlugin>.Info($"NetData length is greater than chunk size: {length} > {chunkSize}");
                continue;
            }

            if (count + length > chunkSize)
            {
                chunks.Enqueue(current.ToArray());
                current.Clear();
                count = 0;
            }

            current.Add(netData);
        }

        if (current.Count > 0)
        {
            chunks.Enqueue([.. current]);
        }

        return chunks;
    }

    /// <summary>
    /// Determines if a given OptionBehaviour is for a custom option.
    /// </summary>
    /// <param name="optionBehaviour">The OptionBehaviour to be tested.</param>
    /// <returns>True if the OptionBehaviour is for a custom options, false otherwise.</returns>
    public static bool IsCustom(this OptionBehaviour optionBehaviour)
    {
        return ModdedOptionsManager.ModdedOptions.Values.Any(
            opt => opt.OptionBehaviour && opt.OptionBehaviour == optionBehaviour);
    }

    /// <summary>
    /// Randomizes a list.
    /// </summary>
    /// <param name="list">The list object.</param>
    /// <typeparam name="T">The type of object the list contains.</typeparam>
    /// <returns>A randomized list made from the original list.</returns>
    public static List<T> Randomize<T>(this List<T> list)
    {
        List<T> randomizedList = [];
        System.Random rnd = new();
        while (list.Count > 0)
        {
            var index = rnd.Next(0, list.Count);
            randomizedList.Add(list[index]);
            list.RemoveAt(index);
        }

        return randomizedList;
    }

    /// <summary>
    /// Darkens a color by a specified amount.
    /// </summary>
    /// <param name="color">The original color.</param>
    /// <param name="amount">A float amount between 0 and 1.</param>
    /// <returns>The darkened color.</returns>
    public static Color DarkenColor(this Color color, float amount = 0.45f)
    {
        return new Color(color.r - amount, color.g - amount, color.b - amount);
    }

    /// <summary>
    /// Gets an alternate color based on the original color.
    /// </summary>
    /// <param name="color">The original color.</param>
    /// <param name="amount">The amount to darken or lighten the original color by between 0.0 and 1.0.</param>
    /// <returns>An alternate color that has been darkened or lightened.</returns>
    public static Color GetAlternateColor(this Color color, float amount = 0.45f)
    {
        return color.IsColorDark() ? LightenColor(color, amount) : DarkenColor(color, amount);
    }

    /// <summary>
    /// Lightens a color by a specified amount.
    /// </summary>
    /// <param name="color">The original color.</param>
    /// <param name="amount">A float amount between 0.0 and 1.0.</param>
    /// <returns>The lightened color.</returns>
    public static Color LightenColor(this Color color, float amount = 0.45f)
    {
        return new Color(color.r + amount, color.g + amount, color.b + amount);
    }

    /// <summary>
    /// Checks if a color is dark.
    /// </summary>
    /// <param name="color">The color to check.</param>
    /// <returns>True if the color is dark, false otherwise.</returns>
    public static bool IsColorDark(this Color color)
    {
        return color.r < 0.5f && color is { g: < 0.5f, b: < 0.5f };
    }

    /// <summary>
    /// Gets the nearest dead body to a player.
    /// </summary>
    /// <param name="playerControl">The player object.</param>
    /// <param name="radius">The radius to search within.</param>
    /// <returns>The dead body if it is found, or null there is none within the radius.</returns>
    public static DeadBody? GetNearestDeadBody(this PlayerControl playerControl, float radius)
    {
        return Helpers
            .GetNearestDeadBodies(playerControl.GetTruePosition(), radius, Helpers.CreateFilter(Constants.NotShipMask))
            .Find(component => component && !component.Reported);
    }

    /// <summary>
    /// Finds the nearest object of a specified type to a player. It will only work if the object has a collider.
    /// </summary>
    /// <param name="playerControl">The player object.</param>
    /// <param name="radius">The radius to search within.</param>
    /// <param name="filter">The contact filter.</param>
    /// <param name="colliderTag">An optional collider tag.</param>
    /// <param name="predicate">Optional predicate to test if the object is valid.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns>The object if it was found, or null if there is none within the radius.</returns>
    public static T? GetNearestObjectOfType<T>(
        this PlayerControl playerControl,
        float radius,
        ContactFilter2D filter,
        string? colliderTag = null,
        Predicate<T>? predicate = null) where T : Component
    {
        return Helpers.GetNearestObjectsOfType<T>(playerControl.GetTruePosition(), radius, filter, colliderTag)
            .Find(predicate ?? (component => component));
    }

    /// <summary>
    /// Gets the closest player that matches the given criteria.
    /// </summary>
    /// <param name="playerControl">The player object.</param>
    /// <param name="includeImpostors">Whether impostors should be included in the search.</param>
    /// <param name="distance">The radius to search within.</param>
    /// <param name="ignoreColliders">Whether colliders should be ignored when searching.</param>
    /// <param name="predicate">Optional predicate to test if the object is valid.</param>
    /// <returns>The closest player if there is one, false otherwise.</returns>
    public static PlayerControl? GetClosestPlayer(
        this PlayerControl playerControl,
        bool includeImpostors,
        float distance,
        bool ignoreColliders = false,
        Predicate<PlayerControl>? predicate = null)
    {
        var filteredPlayers = Helpers.GetClosestPlayers(playerControl, distance, ignoreColliders)
            .Where(
                playerInfo => !playerInfo.Data.Disconnected &&
                              playerInfo.PlayerId != playerControl.PlayerId &&
                              !playerInfo.Data.IsDead &&
                              (includeImpostors || !playerInfo.Data.Role.IsImpostor))
            .ToList();

        return predicate != null ? filteredPlayers.Find(predicate) : filteredPlayers.FirstOrDefault();
    }

    /// <summary>
    /// Fixed version of Reactor's SetOutline.
    /// </summary>
    /// <param name="renderer">The renderer you want to update the outline for.</param>
    /// <param name="color">The outline color.</param>
    public static void UpdateOutline(this Renderer renderer, Color? color)
    {
        renderer.material.SetFloat(ShaderID.Outline, color.HasValue ? 1 : 0);
        renderer.material.SetColor(ShaderID.OutlineColor, color ?? Color.clear);
        renderer.material.SetColor(ShaderID.AddColor, color ?? Color.clear);
    }
}
