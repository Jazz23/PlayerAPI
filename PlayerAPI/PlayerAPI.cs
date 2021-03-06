﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib_K_Relay;
using Lib_K_Relay.GameData;
using Lib_K_Relay.GameData.DataStructures;
using Lib_K_Relay.Interface;
using Lib_K_Relay.Networking;
using Lib_K_Relay.Networking.Packets;
using Lib_K_Relay.Networking.Packets.Client;
using Lib_K_Relay.Networking.Packets.DataObjects;
using Lib_K_Relay.Networking.Packets.Server;
using Lib_K_Relay.Utilities;
using static PlayerAPI.WinAPIs;
using static PlayerAPI.PlayerAPI;

namespace PlayerAPI
{
    public static class PlayerAPI
    {
        public static Random Random = new Random();
        public static List<cPlayer> cPlayers = new List<cPlayer>();
        public static List<Client> Connections = new List<Client>();
        public static List<Player> AllRenderedPlayers = new List<Player>();
        public delegate void OnInventorySwap(Player player, Item[] items);
        public delegate void OnTouchDown(); // Just a handy function that's not already in k relay (probs should add it) - Edit: added it ;)
        public delegate void SteppedOnBag(Entity bag);
        public delegate void OnFameGain(Player player);
        public delegate void OnSelfSwap(Item[] items);
        public delegate void OnPlayerJoin(Player player);
        public delegate void OnPlayerLeave(Player player);
        public delegate void OnBagSpawn(Entity bag);
        public delegate void OnBagDespawn(Entity bag);
        public delegate void StatDataChange(StatData stat);
        public delegate void EntityLeave(Entity entity);
        public delegate void TargetReached();

        private static List<Client> gotoblocks = new List<Client>();

        public static void Start(Proxy proxy)
        {
            proxy.HookPacket<UpdatePacket>(OnUpdate);
            proxy.HookPacket<NewTickPacket>(OnNewTick);
            proxy.HookPacket<InvSwapPacket>(OnInvSwap);
            proxy.HookPacket<GotoAckPacket>(OnGotoAck);

            proxy.ClientDisconnected += (client) =>
            {
                Connections.Remove(client);
                if (client.PlayerData == null) return;
                if (cPlayers.ToList().Select(x => x.Client.PlayerData != null ? x.Client.PlayerData.AccountId : null).Contains(client.PlayerData.AccountId))
                {                                                                                                           // Im kinda a linq boi so this is slightly unnessary but i like less lines
                    cPlayers.Remove(cPlayers.ToList().Single(x => x.Client.PlayerData != null && x.Client.PlayerData.AccountId == client.PlayerData.AccountId));
                }
            };

            proxy.ClientConnected += (client) =>
            {
                Connections.Add(client);
                cPlayers.Add(new cPlayer(client));
            };

            proxy.HookCommand("playerapi", (c, co, a) => c.SendToClient(PluginUtils.CreateNotification(c.ObjectId, "Yup im running")));
        }

        private static void OnGotoAck(Client client, GotoAckPacket packet)
        {
            if (gotoblocks.Contains(client))
            {
                gotoblocks.Remove(client);
                packet.Send = false;
            }
        }

        public static Entity bagBeneathFeet(this Client client)
        {
            foreach (Entity bag in client.Self().Bags.ToList())
            {
                if (Math.Abs(client.PlayerData.Pos.X - bag.Status.Position.X) <= 1 && Math.Abs(client.PlayerData.Pos.Y - bag.Status.Position.Y) <= 1)
                {
                    return bag;
                }
            }

            return null;
        }

        public static Item[] GetItems(Entity entity)
        {
            List<Item> items = new List<Item>();
            foreach (StatData stat in entity.Status.Data.ToList())
            {
                if (stat.IsInventory())
                {
                    items.Add(new Item(stat.Id, stat.IntValue));
                }
            }
            return items.ToArray();
        }

        private static void OnInvSwap(Client client, InvSwapPacket packet)
        {
            //client.Self()
        }

        public static bool IsBag(this Entity entity)
        {
            return entity == null ? false : Enum.IsDefined(typeof(Bags), (short)entity.ObjectType);
        }

        public static bool IsEnemy(this Entity entity)     // ..... TODO
        {
            return false;
        }

        public static bool IsPortal(this Entity entity)     // ..... TODO
        {
            return false;
        }

        public static Entity GetEntity(this Client client, int objectId)
        {
            return client.State.RenderedEntities.FirstOrDefault(x => x.Status.ObjectId == objectId);
        }

        public static Player GetPlayer(this Entity entity)     // ..... TO TEST
        {
            return entity == null ? null : AllRenderedPlayers.FirstOrDefault(x => x.Entity.Status.Data.GetHashCode() == entity.Status.Data.GetHashCode());
        }

        public static void SendGoto(this Client client, Location location)
        {
            gotoblocks.Add(client);
            GotoPacket gpacket = Packet.Create<GotoPacket>(PacketType.GOTO);
            gpacket.Location = location;
            gpacket.ObjectId = client.ObjectId;
            client.SendToClient(gpacket);
        }

        public static bool IsPlayer(this Entity entity)
        {
            return entity == null ? false : Enum.IsDefined(typeof(Classes), (short)entity.ObjectType); // idk why i ahve to do that shorthand if but ok
        }

        public static cPlayer Self(this Client client)
        {
            return cPlayers.FirstOrDefault(x => x.Client.State.ACCID == client.State.ACCID && client.Connected);
        }

        public static bool IsInventory(this StatData data)
        {
            return (7 < data.Id && data.Id < 20) || (70 < data.Id && data.Id < 79);
        }

        public static Location Lerp(Location location, Location target, float step)
        {
            Location loc = location.Clonee();

            if (loc.SquareDistanceTo(target) > step)
            {
                double angle = Math.Atan2(target.Y - loc.Y, target.X - loc.X);
                loc.X += ((float)Math.Cos(angle) * step);
                loc.Y += ((float)Math.Sin(angle) * step);   // HSDAKL;AFJDFKFJDKSJKFDLJK;
            }
            else if (loc.SquareDistanceTo(target) <= step)
            {
                loc = target;
            }
            return loc;
        }

        public static Location Clonee(this Location location)
        {
            return new Location(location.X, location.Y);
        }

        public static float SquareDistanceTo(this Location location, Location target)
        {
            return (float)Math.Sqrt(Math.Pow(location.X - target.X, 2) + Math.Pow(location.Y - target.Y, 2));
        }

        public static void GoBackToRealm(this Client client)
        {
            if (client.State.LastRealm != null)
            {
                client.SendToClient(client.State.LastRealm);
            }
        }

        public static void Oof(this Client client)
        {
            if (client != null && client.Connected) client.SendToClient(PluginUtils.CreateNotification(client.ObjectId, "oof"));
        }

        public static string LastConnection(this Client client)
        {
            return ((MapInfoPacket)client.State["MapInfo"]).Name;
        }

        public static void TeleportTo(this Client client, string name, bool tryanyways = false)
        {
            if (tryanyways || client.Self().LastTeleportTime >= 10100 && !NonUniqueNames.Names.Contains(name))
            {
                client.SendChatMessage("/teleport " + name);
                if (client.Time - client.Self().LastTeleportTime >= 10000)
                {
                    client.Self().LastTeleportTime = client.Time;
                }
            }
        }

        public static void SendChatMessage(this Client client, string message)
        {
            PlayerTextPacket tpacket = Packet.Create<PlayerTextPacket>(PacketType.PLAYERTEXT);
            tpacket.Text = message;
            client.SendToServer(tpacket);
        }

        public static string GetName(this Entity entity)
        {
            StatData stat = entity.Status.Data.FirstOrDefault(x => x.Id == StatsType.Name);
            return stat == null ? null : stat.StringValue;
        }

        public static void SendToNexus(this Client client, string host)
        {
            if (!GameData.Servers.Map.Values.Select(x => x.Address).Contains(host)) return;

            ReconnectPacket rpacket = Packet.Create<ReconnectPacket>(PacketType.RECONNECT);
            rpacket.GameId = -2;
            rpacket.Host = host;
            rpacket.IsFromArena = false;
            rpacket.Key = new byte[0];
            rpacket.KeyTime = client.Time;
            rpacket.Name = "Nexus";
            rpacket.Port = 2050;
            rpacket.Stats = "";
            ReconnectHandler.SendReconnect(client, rpacket);
        }

        public static void EscapeToNexus(this Client client)
        {
            client.SendToServer(Packet.Create<EscapePacket>(PacketType.ESCAPE));
        }

        public static bool IsInNexus(this Client client)
        {
            return client.LastConnection() == "Nexus";
        }

        public static bool IsInRealm(this Client client)
        {
            return client.LastConnection() == "Realm of the Mad God";
        }

        public static Bags GetBag(this Entity entity)
        {
            return Enum.IsDefined(typeof(Bags), (short)entity.ObjectType) ? (Bags)(short)entity.ObjectType : 0;
        }

        public static Entity GetClosestEntity(this Client client)
        {
            Entity closest = null;
            foreach (Entity entity in client.State.RenderedEntities.ToList())
            {
                if (closest == null || client.WhosCloser(closest, entity) == entity)
                {
                    closest = entity;
                }
            }
            return closest;
        }

        public static Player GetClosestPlayer(this Client client)
        {
            Player closest = null;
            foreach (Player player in client.Self().Players)
            {
                if (player.PlayerData.AccountId != client.PlayerData.AccountId && closest == null || client.PlayerData.Pos.SquareDistanceTo(player.PlayerData.Pos) < client.PlayerData.Pos.SquareDistanceTo(closest.PlayerData.Pos))
                {
                    closest = player;
                }
            }
            return closest;
        }

        public static Entity WhosCloser(this Client client, Entity entity1, Entity entity2)
        {
            float distance1 = entity1.Status.Position.SquareDistanceTo(client.PlayerData.Pos);
            float distance2 = entity2.Status.Position.SquareDistanceTo(client.PlayerData.Pos);
            return distance1 < distance2 ? entity1 : entity2;
        }

        public static void FollowEntity(this Client client, Entity entity)
        {
            if (entity != null) client.Self().FollowEntity(entity);
        }

        public static void StopFollowingEntity(this Client client)
        {
            client.Self().StopFollowingEntity();
        }

        public static Entity GetEntityByName(this Client client, string name)
        {
            return client.State.RenderedEntities.FirstOrDefault(x => x.Status.Data.FirstOrDefault(z => z.Id == StatsType.Name && z.StringValue.ToLower() == name.ToLower()) != null);
        }

        public static List<Location> ReverseCopy(this List<Location> list)
        {
            List<Location> newList = new List<Location>();
            newList = list;
            newList.Reverse();
            return newList;
        }

        public static void SendKeyToAllFlash(Virtual_Keys.VirtualKeys key, bool down, params IntPtr[] Exlusions)
        {
            SendKeyToHandles(key, down, GetAllFlashPointers(), Exlusions);
        }

        public static void SendKeyToHandles(Virtual_Keys.VirtualKeys key, bool down, IntPtr[] handles, params IntPtr[] Exlusions)
        {
            foreach (IntPtr handle in handles)
            {
                if (!Exlusions.Contains(handle)) PressKey((int)key, down, handle);
            }
        }

        public static IntPtr[] GetAllFlashPointers()
        {
            var procs = Process.GetProcesses().Where(x => x.ProcessName.ToLower().Contains("flash"));
            if (procs.Count() != 0)
            {
                return procs.Select(x => x.MainWindowHandle).ToArray();
            }
            return null;
        }

        public static void PressKey(int key, bool down, IntPtr Handle)
        {
            SendMessage(Handle, (uint)(down ? 0x100 : 0x101), new IntPtr(key), new IntPtr(0));
        }

        public static void PressKey(Virtual_Keys.VirtualKeys key, bool down, IntPtr Handle)
        {
            SendMessage(Handle, (uint)(down ? 0x100 : 0x101), new IntPtr((int)key), new IntPtr(0));
        }

        public static Size GetSize(this RECT rect)
        {
            return new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        public static void UseAbility(this Client client, byte usetype, Location location = null)   // TO TEST
        {
            if (client.PlayerData.Slot[1] != -1)
            {
                UseItemPacket upacket = Packet.Create<UseItemPacket>(PacketType.USEITEM);
                upacket.ItemUsePos = location == null ? client.PlayerData.Pos : location;
                upacket.SlotObject = client.GetSlotAt(1);
                upacket.Time = client.Time;
                upacket.UseType = usetype;
                client.SendToServer(upacket);
            }
        }

        public static SlotObject GetSlotAt(this Client client, int index)   // TO TEST
        {
            if (client.Self().Self.InventoryIDS[index] != -1)
            {
                SlotObject slot = new SlotObject();
                slot.ObjectId = client.ObjectId;
                slot.ObjectType = client.Self().Self.InventoryIDS[index];
                slot.SlotId = (byte)index;
                return slot;
            }
            return null;
        }

        public static List<int> GetInventoryIDS(this Entity entity)   // TO TEST
        {
            List<int> newList = new List<int>();
            UpdatePacket packet = new UpdatePacket();
            packet.NewObjs = new Entity[1];
            packet.NewObjs[0] = entity;
            PlayerData PlayerData = new PlayerData(entity.Status.ObjectId);
            PlayerData.Parse(packet);
            newList.AddRange(PlayerData.Slot);
            newList.AddRange(PlayerData.BackPack);
            return newList;
        }

        public static List<int> GetInventoryIDS(this PlayerData playerdata)   // TO TEST
        {
            List<int> newList = new List<int>();
            newList.AddRange(playerdata.Slot);
            newList.AddRange(playerdata.BackPack);
            return newList;
        }

        /// <summary>
        /// KILLER BE KILLED MADE THISSSSS
        /// </summary>
        /// <param name="handle"></param>
        public static void PressPlay(IntPtr handle)
        {
            RECT windowRect = new RECT();
            GetWindowRect(handle, ref windowRect);
            var size = windowRect.GetSize();

            int playButtonX = size.Width / 2 + windowRect.Left;
            int playButtonY = (int)((double)size.Height * 0.92) + windowRect.Top;

            POINT relativePoint = new POINT(playButtonX, playButtonY);
            ScreenToClient(handle, ref relativePoint);

            SendMessage(handle, (uint)MouseButton.LeftButtonDown, new IntPtr(0x1), new IntPtr((relativePoint.Y << 16) | (relativePoint.X & 0xFFFF)));
            SendMessage(handle, (uint)MouseButton.LeftButtonUp, new IntPtr(0x1), new IntPtr((relativePoint.Y << 16) | (relativePoint.X & 0xFFFF)));
        }

        public static void SelfTeleport(this Client client)
        {
            client.TeleportTo(client.PlayerData.Name);
        }

        public static void ToggleEntityFollow(this Client client, Entity entity)
        {
            var player = client.Self();
            if (player.TargetEntity == null) client.FollowEntity(entity);
            else client.StopFollowingEntity();
        }

        /// <summary>
        /// May cause dc if the client walks over a wall (its a straight line)
        /// </summary>
        /// <param name="location"></param>
        public static void GotoLocation(this Client client, Location location)
        {
            client.Self().TargetLocation = location;
        }

        public static float ToDegrees(this float radians)
        {
            return (float)(radians * 180 / Math.PI);
        }
        public static float ToRadians(this float degrees)
        {
            return (float)(degrees * Math.PI / 180);
        }

        public static string[] Split(this string Input, string Seperator)
        {
            return Input.Split(new[] { Seperator }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static void SetNextSpawnLocation(this Client client, Location location) // TO BE TESTED
        {
            client.State["NextSpawn"] = location;
        }

        public static void SendToRealm(this Client client, string host, string name = "bingle")
        {
            client.SendToClient(BuidReconnectPacket(0, host, client.Time, name));
        }

        public static ReconnectPacket BuidReconnectPacket(int gameId, string host, int keytime, string name = "bingle", bool isfromarena = false, string stats = "", params byte[] key)
        {
            ReconnectPacket rpacket = Packet.Create<ReconnectPacket>(PacketType.RECONNECT);
            rpacket.GameId = gameId;
            rpacket.Host = host;
            rpacket.IsFromArena = isfromarena;
            rpacket.Key = key;
            rpacket.KeyTime = keytime;
            rpacket.Name = name;
            rpacket.Stats = stats;
            return rpacket;
        }

        public static Player GetClosestPlayerToPoint(this Client client, Location point)
        {
            Player closest = client.Self().Players.First();
            foreach (Player player in client.Self().Players)
            {
                if (player.PlayerData.Pos.SquareDistanceTo(point) < closest.PlayerData.Pos.SquareDistanceTo(point))
                    closest = player;
            }
            return closest;
        }

        public static void TeleportToClosestPlayerToPoint(this Client client, Location point)
        {
            client.TeleportTo(client.GetClosestPlayerToPoint(point).PlayerData.Name);
        }
        //public static void MoveInventory(Slot)

        private static void OnNewTick(Client client, NewTickPacket packet)
        {
            client.Self().Parse(packet);

            foreach (Status stat in packet.Statuses)
            {
                foreach (cPlayer cplayer in cPlayers.ToList())
                {
                    if (cplayer.Client.PlayerData == null) continue;
                    foreach (Player player in cplayer.Players.ToList())
                    {
                        if (player.PlayerData.OwnerObjectId == stat.ObjectId)
                        {
                            // Check for inv changes
                            if (!cPlayers.Select(x =>
                            {
                                if (x.Client.PlayerData == null)
                                {
                                    return null;
                                }
                                else
                                {
                                    return x.Client.PlayerData.Name == client.PlayerData.Name ? null : x.Client.PlayerData.Name;
                                }
                            }).Contains(player.PlayerData.Name)) // Doesn't fire if its a cPlayer that moved the inv
                            {
                                for (int a = 0; a <= stat.Data.Count() - 1; a++)
                                {
                                    StatData thing = stat.Data[a]; // for lack of a better name
                                    if (thing.IsInventory())
                                    {
                                        cplayer.FireInventorySwap(player, stat.Data.Select(x => new Item(x.Id, x.IntValue)).ToArray());
                                        break;
                                    }
                                }
                            }
                            player.PlayerData.Parse(packet);
                            break;
                        }
                    }
                }
            }
        }

        private static void OnUpdate(Client client, UpdatePacket packet)
        {
            if (client.Self() != null) client.Self().Parse(packet);
        }
    }
}