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
    /// Public player (not connected with proxy)
    /// </summary>
    public class Player
    {
        public event StatDataChange OnStatDataChange;
        public Item[] Inventory;
        public List<int> InventoryIDS;   // TO TEST
        public Entity Entity;
        public PlayerData PlayerData;

        public Stopwatch StopWatch = new Stopwatch(); // just for the phermones
        public int Checks = 0;

        public Player(Entity entity)
        {
            if (entity.Status == null) return;
            Entity = entity;
            InventoryIDS = new List<int>();
            UpdatePacket packet = new UpdatePacket();
            packet.NewObjs = new Entity[1];
            packet.NewObjs[0] = entity;
            PlayerData = new PlayerData(entity.Status.ObjectId);
            Inventory = GetItems(entity);
            PlayerData.Parse(packet);
            InventoryIDS = entity.GetInventoryIDS();
        }

        public void Parse(Status status)
        {
            if (status == null) return;
            foreach (StatData data in status.Data)
            {
                StatData olddata = Entity.Status.Data.FirstOrDefault(x => x.Id == data.Id);
                if (olddata != null)
                {
                    olddata.IntValue = data.IntValue;
                    olddata.StringValue = data.StringValue != null ? data.StringValue : null;
                    PlayerData.Parse(data.Id, data.IntValue, data.StringValue);
                    InventoryIDS = PlayerData.GetInventoryIDS();
                }
                OnStatDataChange?.Invoke(data);
            }
            Entity.Status.Position = status.Position;
        }
    }
}
