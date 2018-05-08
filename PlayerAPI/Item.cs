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
