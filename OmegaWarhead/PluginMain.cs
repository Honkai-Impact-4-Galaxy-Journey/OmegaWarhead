using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandSystem;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using MEC;
using Mirror;
using Respawning;
using Respawning.Waves;
using SCPSLAudioApi.AudioCore;
using UnityEngine;

namespace OmegaWarhead
{
    public class PluginMain : Plugin
    {
        public override string Name => "OmegaWarhead";

        public override string Description => "终局核弹";

        public override string Author => "Silver Wolf";

        public override Version Version => new Version("1.0.0");

        public override Version RequiredApiVersion => new Version("0.0.0.0");
        public static CoroutineHandle coroutine;

        public override void Disable()
        {
            WarheadEvents.Detonated -= (WarheadDetonatedEventArgs ev) => { if (!OmegaWarhead.OmegaActivated) Timing.RunCoroutine(ForceEnd()); };
            ServerEvents.RoundRestarted -= () => OmegaWarhead.StopOmega();
            ServerEvents.RoundRestarted -= Music.OnRestartingRound;
            PlayerEvents.InteractingScp330 -= OnPickingScp330;
            ServerEvents.RoundRestarted -= () => Timing.KillCoroutines(coroutine);
            WarheadEvents.Starting -= OnDeadmanActivating;
        }

        public override void Enable()
        {
            WarheadEvents.Detonated += (WarheadDetonatedEventArgs ev) => { if (!OmegaWarhead.OmegaActivated) Timing.RunCoroutine(ForceEnd()); };
            WarheadEvents.Starting += OnDeadmanActivating;
            ServerEvents.RoundRestarted += () => OmegaWarhead.StopOmega();
            ServerEvents.RoundRestarted += Music.OnRestartingRound;
            ServerEvents.RoundRestarted += () => Timing.KillCoroutines(coroutine);
            PlayerEvents.InteractingScp330 += OnPickingScp330;
        }
        public static void OnPickingScp330(PlayerInteractingScp330EventArgs ev)
        {
            System.Random random = new System.Random();
            if (random.Next(1, 8) == 7) ev.CandyType = InventorySystem.Items.Usables.Scp330.CandyKindID.Pink;
        }
        public static void OnDeadmanActivating(WarheadStartingEventArgs ev)
        {
            if (ev.WarheadState.ScenarioType == WarheadScenarioType.DeadmanSwitch)
            {
                ev.IsAllowed = false;
                if (!OmegaWarhead.OmegaActivated) OmegaWarhead.ForceEnd = (Timing.RunCoroutine(OmegaWarhead.OmegaMain()));
            }
        }
        public static IEnumerator<float> ForceEnd()
        {
            yield return Timing.WaitForSeconds(180);
            Round.End(force: true);
        }
    }
    public class FakeConnection : NetworkConnectionToClient
    {
        public FakeConnection(int networkConnectionId) : base(networkConnectionId)
        {
        }

        public override string address => "localhost";

        public override void Send(ArraySegment<byte> segment, int channelId = 0)
        {
        }

        public override void Disconnect()
        {
        }

    }
    public class WarppedAudio
    {
        public AudioPlayerBase Player { get; set; }
        public string Music { get; set; }
        public string Username { get; set; }
        public string Verfiy { get; set; }
    }
    public class Music
    {
        public static List<WarppedAudio> audios = new List<WarppedAudio>();
        public static AudioPlayerBase PlayMusic(string musicname, string name, int vol)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(NetworkManager.singleton.playerPrefab);
            System.Random rand = new System.Random();
            int num = rand.Next(250, 301);
            FakeConnection connection = new FakeConnection(num);
            NetworkServer.AddPlayerForConnection(connection, gameObject);
            ReferenceHub referenceHub = gameObject.GetComponent<ReferenceHub>();
            try
            {
                referenceHub.nicknameSync.DisplayName = name;
                referenceHub.authManager.UserId = $"{musicname}-{num}@server";
            }
            catch
            {

            }
            AudioPlayerBase playerbase = AudioPlayerBase.Get(referenceHub);
            string text = "C:\\Users\\Administrator\\AppData\\Roaming\\SCP Secret Laboratory\\LabAPI" + $"\\{musicname}.ogg";
            playerbase.Enqueue(text, -1);
            playerbase.LogDebug = false;
            playerbase.Volume = vol;
            foreach (Player player in Player.List)
            {
                playerbase.BroadcastTo.Add(player.PlayerId);
            }
            playerbase.Loop = false;
            playerbase.Play(-1);
            referenceHub.roleManager.InitializeNewRole(PlayerRoles.RoleTypeId.Overwatch, PlayerRoles.RoleChangeReason.RemoteAdmin);
            audios.Add(new WarppedAudio { Player = playerbase, Music = musicname, Username = name, Verfiy = $"{musicname}-{num}@server" });
            return playerbase;
        }
        public static void OnRestartingRound()
        {
            foreach (WarppedAudio warppedAudio in audios)
            {
                warppedAudio.Player.Stoptrack(true);
            }
            audios.Clear();
        }
    }
    public class OmegaWarhead
    {
        public static List<Player> Helikopter = new List<Player>();
        public static List<Player> Shelted = new List<Player>();
        public static bool OmegaActivated = false;
        public static CoroutineHandle ForceEnd;
        public static AudioPlayerBase playerBase;
        public static void StopOmega()
        {
            if (OmegaActivated)
            {
                Shelted.Clear();
                Helikopter.Clear();
                Timing.KillCoroutines(ForceEnd);
                playerBase.Stoptrack(true);
                Cassie.Message("pitch_0.9 Omega Warhead detonation stopped");
                foreach (Door door in Door.List)
                {
                    if (door.DoorName == LabApi.Features.Enums.DoorName.LczCheckpointA ||
                        door.DoorName == LabApi.Features.Enums.DoorName.LczCheckpointB ||
                        door.DoorName == LabApi.Features.Enums.DoorName.Lcz914Gate ||
                        door.DoorName == LabApi.Features.Enums.DoorName.HczCheckpoint ||
                        door.DoorName == LabApi.Features.Enums.DoorName.EzGateA ||
                        door.DoorName == LabApi.Features.Enums.DoorName.EzGateB)
                    {
                        door.IsLocked = false;
                    }
                }
                foreach (Room room in Room.List)
                {
                    foreach (var light in room.AllLightControllers)
                    {
                        light.OverrideLightsColor = Color.clear;
                    }
                }
                foreach (Elevator elevator in from e in Elevator.List
                                              where e.Group == Interactables.Interobjects.ElevatorGroup.GateA || e.Group == Interactables.Interobjects.ElevatorGroup.GateB
                                              select e)
                {
                    elevator.UnlockAllDoors();
                }
                OmegaActivated = false;
            }
        }
        public static void StopEndRound()
        {
            Timing.KillCoroutines(OmegaWarhead.ForceEnd);
            OmegaWarhead.playerBase.Stoptrack(true);
            OmegaWarhead.OmegaActivated = false;
            foreach (Room room in Room.List)
            {
                foreach (var light in room.AllLightControllers)
                {
                    light.OverrideLightsColor = Color.clear;
                }
            }
        }
        public static IEnumerator<float> ForceEndRound()
        {
            if (OmegaActivated) { yield break; }
            yield return Timing.WaitForSeconds(3);
            Warhead.Stop();
            foreach (Room room in Room.List)
            {
                foreach (var light in room.AllLightControllers)
                {
                    light.OverrideLightsColor = Color.cyan;
                }
            }
            Warhead.IsLocked = true;
            OmegaActivated = true;
            playerBase = Music.PlayMusic("Omega", "Omega核弹警报", 20);
            yield return Timing.WaitForSeconds(184);
            foreach (Player player in Player.List)
            {
                if (player.IsAlive) player.Kill("在Omega核弹中蒸发(强制终局)");
            }
            foreach (Room room in Room.List)
            {
                foreach (var light in room.AllLightControllers)
                {
                    light.OverrideLightsColor = Color.blue;
                }
            }
        }
        public static IEnumerator<float> OmegaMain()
        {
            if (OmegaActivated) { yield break; }
            OmegaActivated = true;
            Shelted.Clear();
            Helikopter.Clear();
            yield return Timing.WaitForSeconds(3);
            Warhead.Stop();
            foreach (Room room in Room.List)
            {
                foreach (var light in room.AllLightControllers)
                {
                    light.OverrideLightsColor = Color.cyan;
                }
            }
            Warhead.IsLocked = true;
            Server.SendBroadcast("<b><color=red>OMEGA核弹已启用.</color></b>\n请搭乘撤离直升机或前往地下掩体逃生.", 20);
            playerBase = Music.PlayMusic("Omega", "Omega核弹警报", 20);
            yield return Timing.WaitForSeconds(124);
            foreach (Door door in Door.List)
            {
                if (door.DoorName == LabApi.Features.Enums.DoorName.LczCheckpointA ||
                    door.DoorName == LabApi.Features.Enums.DoorName.LczCheckpointB ||
                    door.DoorName == LabApi.Features.Enums.DoorName.Lcz914Gate ||
                    door.DoorName == LabApi.Features.Enums.DoorName.HczCheckpoint ||
                    door.DoorName == LabApi.Features.Enums.DoorName.EzGateA ||
                    door.DoorName == LabApi.Features.Enums.DoorName.EzGateB)
                {
                    door.IsOpened = true;
                    door.Lock(Interactables.Interobjects.DoorUtils.DoorLockReason.Warhead, true);
                }
            }
            yield return Timing.WaitForSeconds(38);
            Server.SendBroadcast("撤离直升机将在12s后到达", 10, shouldClearPrevious: true);
            WaveManager.TryGet<NtfSpawnWave>(out var wave);
            WaveUpdateMessage.ServerSendUpdate(wave, UpdateMessageFlags.Trigger);
            yield return Timing.WaitForSeconds(12);
            Helikopter.AddRange(from p in Player.ReadyList
                          where Vector3.Distance(p.Position, new Vector3(126, 295, -44)) <= 8
                          select p);
            Helikopter.ForEach(p => { p.EnableEffect<Flashed>(1, 15); p.EnableEffect<Ensnared>(1, 15); p.IsGodModeEnabled = true; });
            yield return Timing.WaitForSeconds(10);
            Shelted.AddRange(from p in Player.ReadyList
                             where p.Room?.Name == MapGeneration.RoomName.EzEvacShelter
                             select p);
            Shelted.ForEach(p => { p.Position = (new Vector3(29, 293, -26)); p.EnableEffect<Flashed>(1, 5); });
            foreach (Player player in Player.List)
            {
                if (!(Shelted.Contains(player) || Helikopter.Contains(player)) && player.IsAlive) player.Kill("在Omega核弹中蒸发");
            }
            foreach (Room room in Room.List)
            {
                foreach (var light in room.AllLightControllers)
                {
                    light.OverrideLightsColor = Color.blue;
                }
            }
            foreach (Elevator elevator in from e in Elevator.List
                                          where e.Group == Interactables.Interobjects.ElevatorGroup.GateA || e.Group == Interactables.Interobjects.ElevatorGroup.GateB
                                          select e)
            {
                elevator.LockAllDoors();
            }
            Helikopter.ForEach(p => { p.IsGodModeEnabled = false; });
            yield return Timing.WaitForSeconds(180);
            Round.End(force:true);
        }
    }
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class StartEndingRound : ICommand
    {
        public string Command => "startendround";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "force end round in 180s";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!OmegaWarhead.OmegaActivated) OmegaWarhead.ForceEnd = Timing.RunCoroutine(OmegaWarhead.ForceEndRound());
            response = "Done!";
            return true;
        }
    }
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class StopEndingRound : ICommand
    {
        public string Command => "stopendround";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "stop end round";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            OmegaWarhead.StopOmega();
            response = "Done!";
            return true;
        }
    }
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ActivateOmega : ICommand
    {
        public string Command => "activateomega";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "启动Omega核弹";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!OmegaWarhead.OmegaActivated) OmegaWarhead.ForceEnd = Timing.RunCoroutine(OmegaWarhead.OmegaMain());
            response = "Done!";
            return true;
        }
    }
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class StopOmega : ICommand
    {
        public string Command => "stopomega";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "关闭Omega核弹";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            OmegaWarhead.StopOmega();
            response = "Done!";
            return true;
        }
    }
}
