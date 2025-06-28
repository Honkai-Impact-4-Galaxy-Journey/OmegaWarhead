using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandSystem;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using MEC;
using Mirror;
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

        public override void Disable()
        {
            WarheadEvents.Detonated -= (WarheadDetonatedEventArgs ev) => OmegaWarhead.ForceEnd = (Timing.RunCoroutine(OmegaWarhead.ForceEndRound()));
        }

        public override void Enable()
        {
            WarheadEvents.Detonated += (WarheadDetonatedEventArgs ev) => OmegaWarhead.ForceEnd = (Timing.RunCoroutine(OmegaWarhead.ForceEndRound()));
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
        public static List<CoroutineHandle> coroutines = new List<CoroutineHandle>();
        public static bool OmegaActivated = false;
        public static CoroutineHandle ForceEnd;
        public static AudioPlayerBase playerBase;
        public static void StopOmega()
        {
            if (OmegaActivated)
            {
                foreach (CoroutineHandle coroutineHandle in coroutines) Timing.KillCoroutines(coroutineHandle);
                playerBase.Stoptrack(true);
                Cassie.Message("pitch_0.9 Omega Warhead detonation stopped");
                foreach (Room room in Room.List)
                {
                    foreach (var light in room.AllLightControllers)
                    {
                        light.OverrideLightsColor = Color.clear;
                    }
                }
                OmegaActivated = false;
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
            playerBase = Music.PlayMusic("Omega", "Omega核弹警报", 40);
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
        [CommandHandler(typeof(GameConsoleCommandHandler))]
        [CommandHandler(typeof(RemoteAdminCommandHandler))]
        public class StartEndingRound : ICommand
        {
            public string Command => "startendround";

            public string[] Aliases => Array.Empty<string>();

            public string Description => "force end round in 180s";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                OmegaWarhead.ForceEnd = Timing.RunCoroutine(OmegaWarhead.ForceEndRound());
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
                response = "Done!";
                return true;
            }
        }
    }
}
