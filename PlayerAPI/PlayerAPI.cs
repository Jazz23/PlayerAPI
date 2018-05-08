using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                if (cPlayers.Select(x => x.Client.PlayerData != null ? x.Client.PlayerData.AccountId : null).Contains(client.PlayerData.AccountId))
                {                                                                                                           // Im kinda a linq boi so this is slightly unnessary but i like less lines
                    cPlayers.Remove(cPlayers.Single(x => x.Client.PlayerData != null && x.Client.PlayerData.AccountId == client.PlayerData.AccountId));
                }
            };

            proxy.ClientConnected += (client) =>
            {
                Connections.Add(client);
                cPlayers.Add(new cPlayer(client));
            };
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
            cPlayer player = cPlayers.FirstOrDefault(x => x.Client.State.ACCID == client.State.ACCID && client.Connected);
            return player != null ? player : null;
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

        public static void TeleportTo(this Client client, string name)
        {
            PlayerTextPacket tpacket = Packet.Create<PlayerTextPacket>(PacketType.PLAYERTEXT);
            tpacket.Text = "/teleport " + name;
            client.SendToServer(tpacket);
        }

        public static string Name(this Entity entity)
        {
            StatData stat = entity.Status.Data.FirstOrDefault(x => x.Id == StatsType.Name);
            return stat == null ? null : stat.StringValue;
        }

        //public static void MoveInventory(Slot)

        private static void OnNewTick(Client client, NewTickPacket packet)
        {
            client.Self().Parse(packet);

            foreach (Status stat in packet.Statuses.ToList())
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