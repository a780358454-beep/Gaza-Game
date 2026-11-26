using System;
using System.Collections.Generic;
using Godot;

namespace GazaGame.Core
{
    /// <summary>
    /// Network manager supporting ENet LAN discovery on UDP 4242, direct online hosting on 7777,
    /// and automatic AI Squad Backfill for 4-man squads.
    /// </summary>
    public partial class NetworkManager : Node
    {
        public static NetworkManager Instance { get; private set; } = null!;

        [Signal]
        public delegate void PeerJoinedEventHandler(long peerId, string playerName);

        [Signal]
        public delegate void PeerLeftEventHandler(long peerId);

        [Signal]
        public delegate void ServerDiscoveredEventHandler(string ipAddress, string serverName, int playerCount);

        private ENetMultiplayerPeer? _peer;
        private PacketPeerUdp? _broadcastUdp;
        private double _broadcastTimer = 0.0;
        private const double BroadcastInterval = 1.0;

        public bool IsHost { get; private set; } = false;
        public Dictionary<long, string> ConnectedPlayers { get; } = new();
        public List<string> ActiveAIBots { get; } = new();

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                ProcessMode = ProcessModeEnum.Always;
                Multiplayer.PeerConnected += OnPeerConnected;
                Multiplayer.PeerDisconnected += OnPeerDisconnected;
            }
            else
            {
                QueueFree();
            }
        }

        public override void _Process(double delta)
        {
            if (IsHost && _broadcastUdp != null)
            {
                _broadcastTimer += delta;
                if (_broadcastTimer >= BroadcastInterval)
                {
                    _broadcastTimer = 0.0;
                    BroadcastLanPresence();
                }
            }
            else if (!IsHost && _broadcastUdp != null)
            {
                PollLanDiscovery();
            }
        }

        /// <summary>
        /// Starts hosting a multiplayer session on port 7777 and initializes LAN broadcasting.
        /// </summary>
        public Error HostGame(int port = GameConfig.GameServerPort, int maxClients = 4)
        {
            _peer = new ENetMultiplayerPeer();
            var err = _peer.CreateServer(port, maxClients);
            if (err != Error.Ok)
            {
                GD.PrintErr($"Failed to create host server on port {port}: {err}");
                return err;
            }

            Multiplayer.MultiplayerPeer = _peer;
            IsHost = true;
            ConnectedPlayers[1] = "Host";

            // Initialize UDP broadcast socket
            _broadcastUdp = new PacketPeerUdp();
            _broadcastUdp.SetBroadcastEnabled(true);
            _broadcastUdp.SetDestAddress("255.255.255.255", GameConfig.LanDiscoveryPort);

            GD.Print($"[Network] Host started on port {port}. Running AI Backfill...");
            AISquadBackfill();
            return Error.Ok;
        }

        /// <summary>
        /// Connects to a direct IP or LAN server.
        /// </summary>
        public Error JoinGame(string ip, int port = GameConfig.GameServerPort)
        {
            _peer = new ENetMultiplayerPeer();
            var err = _peer.CreateClient(ip, port);
            if (err != Error.Ok)
            {
                GD.PrintErr($"Failed to join server {ip}:{port}: {err}");
                return err;
            }

            Multiplayer.MultiplayerPeer = _peer;
            IsHost = false;
            GD.Print($"[Network] Connecting to {ip}:{port}...");
            return Error.Ok;
        }

        /// <summary>
        /// Starts listening for LAN broadcasts on UDP 4242.
        /// </summary>
        public void StartLanDiscovery()
        {
            _broadcastUdp = new PacketPeerUdp();
            var err = _broadcastUdp.Bind(GameConfig.LanDiscoveryPort);
            if (err != Error.Ok)
            {
                GD.PrintErr($"Failed to bind LAN discovery socket: {err}");
            }
        }

        private void BroadcastLanPresence()
        {
            if (_broadcastUdp == null) return;
            string msg = $"GAZA_SERVER|غزة - معركة التقاطع|{ConnectedPlayers.Count}|{GameConfig.GameServerPort}";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            _broadcastUdp.PutPacket(bytes);
        }

        private void PollLanDiscovery()
        {
            if (_broadcastUdp == null || _broadcastUdp.GetAvailablePacketCount() <= 0) return;

            while (_broadcastUdp.GetAvailablePacketCount() > 0)
            {
                byte[] packet = _broadcastUdp.GetPacket();
                string senderIp = _broadcastUdp.GetPacketIp();
                string msg = System.Text.Encoding.UTF8.GetString(packet);
                if (msg.StartsWith("GAZA_SERVER"))
                {
                    string[] parts = msg.Split('|');
                    if (parts.Length >= 4 && int.TryParse(parts[2], out int count))
                    {
                        EmitSignal(SignalName.ServerDiscovered, senderIp, parts[1], count);
                    }
                }
            }
        }

        private void OnPeerConnected(long id)
        {
            GD.Print($"[Network] Peer {id} connected.");
            ConnectedPlayers[id] = $"Player_{id}";
            EmitSignal(SignalName.PeerJoined, id, ConnectedPlayers[id]);

            if (IsHost)
            {
                // Re-evaluate AI Squad Backfill
                AISquadBackfill();
            }
        }

        private void OnPeerDisconnected(long id)
        {
            GD.Print($"[Network] Peer {id} disconnected.");
            ConnectedPlayers.Remove(id);
            EmitSignal(SignalName.PeerLeft, id);

            if (IsHost)
            {
                AISquadBackfill();
            }
        }

        /// <summary>
        /// Backfills missing squad slots with intelligent SquadAI bots up to 4 members.
        /// If a human joins, seamless handover removes one AI bot.
        /// </summary>
        public void AISquadBackfill()
        {
            int humanCount = ConnectedPlayers.Count;
            int neededBots = Math.Max(0, GameConfig.MaxSquadMembers - humanCount);

            GD.Print($"[Network] AISquadBackfill -> Humans: {humanCount}, Required Bots: {neededBots}");

            ActiveAIBots.Clear();
            var roster = GameConfig.SquadRoster;
            for (int i = humanCount; i < GameConfig.MaxSquadMembers; i++)
            {
                if (i < roster.Length)
                {
                    ActiveAIBots.Add(roster[i].Id);
                    GD.Print($"  -> Backfilled Squad Slot [{i+1}] with AI Bot: {roster[i].NameAr} ({roster[i].RoleAr})");
                }
            }
        }

        public void DisconnectSession()
        {
            _peer?.Close();
            _peer = null;
            _broadcastUdp?.Close();
            _broadcastUdp = null;
            ConnectedPlayers.Clear();
            ActiveAIBots.Clear();
            IsHost = false;
        }
    }
}
