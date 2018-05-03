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
        public static List<cPlayer> cPlayers = new List<cPlayer>();
        public delegate void OnInventorySwap(Player player, Item[] items);
        public delegate void OnTouchDown(); // Just a handy function that's not already in k relay (probs should add it)
        public delegate void SteppedOnBag(Entity bag);
        public delegate void OnFameGain(Player player);
        public delegate void OnSelfSwap(Item[] items);
        public delegate void OnPlayerJoin(Player player);
        public delegate void OnPlayerLeave(Player player);
        public delegate void OnBagSpawn(Entity bag);
        public delegate void OnBagDespawn(Entity bag);

        public static void Start(Proxy proxy)
        {
            proxy.HookPacket<UpdatePacket>(onUpdate);
            proxy.HookPacket<NewTickPacket>(onNewTick);
            proxy.HookPacket<InvSwapPacket>(onInvSwap);

            proxy.ClientDisconnected += (client) =>
            {
                if (client.PlayerData == null) return;
                if (cPlayers.Select(x => { if (x.Client.PlayerData != null) return x.Client.PlayerData.AccountId; else return null; }).Contains(client.PlayerData.AccountId))
                {                                                                                                           // Im kinda a linq boi so this is slightly unnessary but i like less lines
                    cPlayers.Remove(cPlayers.Single(x => x.Client.PlayerData != null && x.Client.PlayerData.AccountId == client.PlayerData.AccountId));
                }
            };

            proxy.ClientConnected += (client) =>
            {
                cPlayers.Add(new cPlayer(client));
            };
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

        private static void onInvSwap(Client client, InvSwapPacket packet)
        {
            //client.Self()
        }

        public static bool IsBag(this Entity entity)
        {
            return Enum.IsDefined(typeof(Bags), (short)entity.ObjectType);
        }

        //public static void MoveInventory(Slot)

        private static void onNewTick(Client client, NewTickPacket packet)
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

        public static bool IsInventory(this StatData data)
        {
            return (7 < data.Id && data.Id < 20) || (70 < data.Id && data.Id < 79);
        }

        private static void onUpdate(Client client, UpdatePacket packet)
        {
            if (client.Self() != null) client.Self().Parse(packet);
        }

        public static bool IsPlayer(this Entity entity)
        {
            return Enum.IsDefined(typeof(Classes), (Int16)entity.ObjectType);
        }

        public static cPlayer Self(this Client client)
        {
            cPlayer player = cPlayers.FirstOrDefault(x => x.Client.State.ACCID == client.State.ACCID && client.Connected);
            return player != null ? player : null;
        }
    }

    /// <summary>
    /// Connected player from the proxy.
    /// </summary>
    public class cPlayer
    {
        #region Client Events
        public event OnInventorySwap OnInventorySwap;
        public event OnSelfSwap OnSelfSwap;
        public event OnTouchDown OnTouchDown;
        public event SteppedOnBag SteppedOnBag;
        public event OnFameGain OnFameGain;
        public event OnPlayerLeave OnPlayerLeave;
        public event OnPlayerJoin OnPlayerJoin;
        public event OnBagSpawn OnBagSpawn;
        public event OnBagDespawn OnBagDespawn;
        #endregion

        public List<Player> Players = new List<Player>();
        public Player Self;
        public List<Entity> Bags = new List<Entity>();
        public Client Client;



        public cPlayer(Client client)
        {
            Client = client;
        }

        public void FireInventorySwap(Player player, Item[] items)
        {
            OnInventorySwap?.Invoke(player, items);

            foreach (Item item in items.ToList())
            {
                Self.Inventory = Self.Inventory.Select(x =>
                {
                    return x.Slot == item.Slot ? item : x;
                }).ToArray();
            }
        }

        public void HitTheGround(Entity entity)
        {
            Self = new Player(entity);
            Client.PlayerData.Pos = entity.Status.Position;
            OnTouchDown?.Invoke();
        }

        public void Parse(UpdatePacket packet)
        {
            Entity self = packet.NewObjs.FirstOrDefault(x => x.Status.Data.FirstOrDefault(z => z.Id == StatsType.AccountId && z.StringValue == Client.State.ACCID) != null); // The first new object whos data has a Status whos id is AccountId and equals the accound id of the client that's not null;
            if (self != null)
            {
                HitTheGround(self);
            }

            foreach (Entity entity in packet.NewObjs)
            {
                if (entity.IsPlayer())
                {
                    Player freshy = new Player(entity);
                    Players.Add(freshy);
                    OnPlayerJoin?.Invoke(freshy);
                }
                else if (entity.IsBag())
                {
                    Bags.Add(entity);
                    OnBagSpawn?.Invoke(entity);
                }
            }

            foreach (int entity in packet.Drops.ToList())
            {
                Player player = Players.FirstOrDefault(x => x.Entity.Status.ObjectId == entity);
                Entity bag = Bags.FirstOrDefault(x => x.Status.ObjectId == entity);
                if (player != null)
                {
                    Players.Remove(player);
                    OnPlayerLeave?.Invoke(player);
                }
                else if (bag != null)
                {
                    Bags.Remove(bag);
                    OnBagDespawn?.Invoke(bag);
                }
            }
        }

        public void Parse(NewTickPacket packet)
        {

        }
    }

    /// <summary>
    /// Public player (not connected with proxy)
    /// </summary>
    public class Player
    {
        public Item[] Inventory;
        public Entity Entity;
        public PlayerData PlayerData;

        public Stopwatch StopWatch = new Stopwatch(); // just for the phermones
        public int Checks = 0;

        public Player(Entity entity)
        {
            if (entity.Status == null) return;
            Entity = entity;
            UpdatePacket packet = new UpdatePacket();
            packet.NewObjs = new Entity[1];
            packet.NewObjs[0] = entity;
            PlayerData = new PlayerData(entity.Status.ObjectId);
            Inventory = GetItems(entity);
            PlayerData.Parse(packet);
        }
    }

    /// <summary>
    /// An item class representing in game items in other players inventory.
    /// </summary>
    public class Item
    {
        public int ObjectType;
        public string Name;
        public int Slot;
        public ItemStructure _Item;

        public Item(int id, int objecttype)
        {
            Slot = id > 70 ? id - 59 : id - 8;
            ObjectType = objecttype;
            //try
            //{
            //    _Item = GameData.Items.ByID((ushort)objecttype);
            //}
            //catch
            //{
            //    _Item = null;
            //}
            Name = _Item != null ? _Item.Name : null;
        }

        public void MoveTo(int slot, Client client, bool ground = false, bool bag = false)
        {
            SlotObject slot1 = new SlotObject();
            SlotObject slot2 = new SlotObject();
            slot1.ObjectId = client.ObjectId;
            slot1.ObjectType = ObjectType;
            slot1.SlotId = (byte)Slot;
            if (!ground && !bag)
            {
                slot2.ObjectId = client.ObjectId;
                slot2.ObjectType = slot < 12 ? client.PlayerData.Slot[slot] : client.PlayerData.BackPack[slot - 11];
                slot2.SlotId = (byte)slot;
            }
            else if (!ground && bag)
            {
                slot2.ObjectId = client.ObjectId;
                slot2.ObjectType = -1;
                slot2.SlotId = 1;
            }
            else
            {
                InvDropPacket dropPacket = (InvDropPacket)Packet.Create(PacketType.INVDROP);
                dropPacket.Slot = slot1;
                client.SendToServer(dropPacket);
                return;
            }

            InvSwapPacket swapPacket = (InvSwapPacket)Packet.Create(PacketType.INVSWAP);
            swapPacket.Position = client.PlayerData.Pos;
            swapPacket.SlotObject1 = slot1;
            swapPacket.SlotObject2 = slot2;
            swapPacket.Time = client.Time;
            client.SendToServer(swapPacket);
        }
    }
}