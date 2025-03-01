﻿using System.Linq;
using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Mira;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Map;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Example.Buttons.Freezer;
using MiraAPI.Example.Roles;
using MiraAPI.Utilities;
using Reactor.Utilities;

namespace MiraAPI.Example;

public static class ExampleEventHandlers
{
    public static void Initialize()
    {
        // You can register event handlers with the MiraEventManager class.
        MiraEventManager.RegisterEventHandler<BeforeMurderEvent>(@event =>
        {
            Logger<ExamplePlugin>.Info($"{@event.Source.Data.PlayerName} is about to kill {@event.Target.Data.PlayerName}");
        });

        MiraEventManager.RegisterEventHandler<AfterMurderEvent>(@event =>
        {
            Logger<ExamplePlugin>.Info($"{@event.Source.Data.PlayerName} has killed {@event.Target.Data.PlayerName}");
        });

        MiraEventManager.RegisterEventHandler<CompleteTaskEvent>(@event =>
        {
            Logger<ExamplePlugin>.Info($"{@event.Player.Data.PlayerName} completed {@event.Task.TaskType.ToString()}");
        });
    }

    [RegisterEvent]
    public static void StartMeetingEvent(StartMeetingEvent _)
    {
        foreach (var plr in PlayerControl.AllPlayerControls)
        {
            if (plr.Data.Role is MayorRole)
            {
                plr.GetVoteData().AddVotes(1);
            }
        }
    }

    [RegisterEvent(15)]
    public static void HandleVoteEvent(HandleVoteEvent @event)
    {
        if (@event.VoteData.Owner?.Data.Role is not NeutralKillerRole) return;

        @event.VoteData.SetVotesRemaining(0);

        for (var i = 0; i < 5; i++)
        {
            @event.VoteData.VoteForPlayer(@event.TargetId);
        }

        foreach (var plr in PlayerControl.AllPlayerControls.ToArray().Where(player => player != @event.VoteData.Owner))
        {
            plr.GetVoteData().VotedPlayers.Clear();
            plr.GetVoteData().VotesRemaining = 0;
        }

        @event.Cancel();
    }

    // Events can be registered using an attribute as well.
    [RegisterEvent]
    public static void UpdateSystemEventHandler(UpdateSystemEvent @event)
    {
        Logger<ExamplePlugin>.Error(@event.SystemType.ToString());
    }

    // Example event handler
    [RegisterEvent(1)]
    public static void FreezeButtonClickHandler(MiraButtonClickEvent<FreezeButton> @event)
    {
        Logger<ExamplePlugin>.Warning("Freeze button clicked!");

        if (PlayerControl.LocalPlayer.Data.PlayerName != "stupid") return;

        @event.Cancel();
        @event.Button.SetTimer(15f);
    }

    // Example event handler
    [RegisterEvent]
    public static void FreezeButtonCancelledHandler(MiraButtonCancelledEvent<FreezeButton> @event)
    {
        Logger<ExamplePlugin>.Warning("Freeze button cancelled!");
        @event.Button.OverrideName("Freeze Canceled");
    }
}
