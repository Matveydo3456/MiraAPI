﻿using MiraAPI.GameOptions.Attributes;

namespace MiraAPI.Example;

public class SomeRandomNonGroupClass
{
    [ModdedStringOption("Yeah, idk", ["Idk 1", "idk 2", "idk 3", "idk 4"])] public static int yeaIdk { get; set; } = 3;
    [ModdedNumberOption("Aw man", min: 45, max: 95)] public float sussyLevel { get; set; } = 63;
}