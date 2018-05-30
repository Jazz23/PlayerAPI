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
        public event TargetReached TargetReached;
        public event EntityLeave OnEntityLeave;
        #endregion

        public List<Player> Players = new List<Player>();
        public List<Entity> Bags = new List<Entity>();
        public List<Entity> Portals = new List<Entity>();
        public List<Entity> Enemies = new List<Entity>();
        public Player Self;
        public Client Client;
        public Location TargetLocation;
        public Entity TargetEntity;
        public int LastTeleportTime = -10000;

        public Dictionary<Entity, List<Location>> EntityPaths = new Dictionary<Entity, List<Location>>();
        public List<Location> TargetEntityPathCopyThing = null;



        public cPlayer(Client client)
        {
            Client = client;
            OnEntityLeave += OnEntityLeaveMeme;
        }

        public void FireInventorySwap(Player player, Item[] items)
        {
            OnInventorySwap?.Invoke(player, items);

            foreach (Item item in items.ToList())
            {
                Self.Inventory = Self.Inventory.Select(x => x.Slot == item.Slot ? item : x).ToArray();
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
                if (Client.State["NextSpawn"] != null)
                {
                    self.Status.Position = Client.State["NextSpawn"] as Location;
                    Client.State["NextSpawn"] = null;
                }
            }

            foreach (Entity entity in packet.NewObjs)
            {
                if (entity.IsPlayer())
                {
                    Player freshy = new Player(entity);
                    Players.Add(freshy);
                    if (!AllRenderedPlayers.Select(x => x.Entity.Status.Data.GetHashCode()).Contains(freshy.Entity.Status.Data.GetHashCode())) AllRenderedPlayers.Add(freshy);
                    OnPlayerJoin?.Invoke(freshy);
                }
                else if (entity.IsBag())
                {
                    Bags.Add(entity);
                    OnBagSpawn?.Invoke(entity);
                }
                else if (entity.IsPortal())
                {

                }
                else if (entity.IsEnemy())
                {

                }
            }

            foreach (int objectId in packet.Drops.ToList())
            {
                Entity entity = Client.GetEntity(objectId);
                OnEntityLeave?.Invoke(entity);
                if (entity.IsPlayer())
                {
                    //Player player = entity.GetPlayer();
                    Player player = Client.Self().Players.First(x => x.Entity.Status.ObjectId == entity.Status.ObjectId); // Don't know if ic an just do entity == entity too l8z two test
                    
                    if (!cPlayers.SelectMany(x => x.Players).Contains(player)) AllRenderedPlayers.Remove(player);
                    Players.Remove(player);
                    OnPlayerLeave?.Invoke(player);
                }
                else if (entity.IsBag())
                {
                    Bags.Remove(entity);
                    OnBagDespawn?.Invoke(entity);
                }
                else if (entity.IsPortal())
                {

                }
                else if (entity.IsEnemy())
                {

                }
            }
        }

        public void Parse(NewTickPacket packet)
        {
            foreach (Status status in packet.Statuses)
            {
                var thing = Players.FirstOrDefault(x => x.PlayerData.OwnerObjectId == status.ObjectId);
                if (thing != null) thing.Parse(status);
                Entity entity = Client.GetEntity(status.ObjectId);
                if (entity == null) continue;
                if (!EntityPaths.ContainsKey(entity)) EntityPaths[entity] = new List<Location>();
                EntityPaths[entity].Add(status.Position);
                if (TargetEntityPathCopyThing != null && TargetEntity?.Status.ObjectId == entity.Status.ObjectId)
                {
                    TargetEntityPathCopyThing.Add(status.Position);
                }
            }

            if (TargetEntityPathCopyThing?.Count() > 0)
            {
                TargetLocation = TargetEntityPathCopyThing.First();
            }

            if (TargetLocation != null)
            {
                Location result = Lerp(Client.PlayerData.Pos, TargetLocation, Client.PlayerData.TilesPerTick());
                Client.SendGoto(result);
                if (result.ToString() == TargetLocation.ToString())
                {
                    TargetLocation = null;
                    if (TargetEntityPathCopyThing?.Count() > 0)
                    {
                        OnTargetReached();
                    }
                    else
                    {
                        TargetReached?.Invoke();
                    }
                }
            }
        }

        public void FireThing()
        {
            TargetReached?.Invoke();
        }

        public void FollowEntity(Entity entity)
        {
            TargetEntity = entity;
            TargetEntityPathCopyThing = new List<Location>();
            TargetEntityPathCopyThing.Add(TargetEntity.Status.Position);
            TargetLocation = TargetEntity.Status.Position;
        }

        public void StopFollowingEntity()
        {
            TargetEntity = null;
            TargetEntityPathCopyThing = null;
            TargetLocation = null;
        }

        private void OnEntityLeaveMeme(Entity ent)
        {
            if (ent?.Status.ObjectId == TargetEntity?.Status.ObjectId && TargetEntity != null)
            {
                StopFollowingEntity();
            }
        }

        private void OnTargetReached()
        {
            TargetEntityPathCopyThing.RemoveAt(0);
            if (TargetEntityPathCopyThing.First() == TargetEntity?.Status.Position && TargetEntity != null)
            {
                TargetEntityPathCopyThing.Clear();
                TargetEntityPathCopyThing.Add(TargetEntity.Status.Position);
            }
        }
    }
}
