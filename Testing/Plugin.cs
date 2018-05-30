using Lib_K_Relay;
using Lib_K_Relay.Interface;
using Lib_K_Relay.Networking;
using Lib_K_Relay.Networking.Packets;
using Lib_K_Relay.Networking.Packets.Client;
using Lib_K_Relay.Networking.Packets.Server;
using Lib_K_Relay.Networking.Packets.DataObjects;
using Lib_K_Relay.Utilities;
using PlayerAPI;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using static PlayerAPI.PlayerAPI;

namespace Plugin
{
    public class Plugin : IPlugin
    {
        public string GetAuthor()
        {
            return "Jazz";
        }

        public string GetName()
        {
            return "PlayerAPI Test";
        }

        public string GetDescription()
        {
            return "just misc palyer api testing";
        }

        public string[] GetCommands()
        {
            return new string[]
            {
                "nuggets are tasty if they're the right ones"
            };
        }

        public static Random Random = new Random();
        public void Initialize(Proxy proxy)
        {
            proxy.OnTouchDown += client =>
            {
                if (client.LastConnection() == "Nexus")
                {
                    float ran = Random.Next(-300, 300) * 0.01f;
                    float idk = Random.Next(0, 50) * 0.01f + 1f;
                    client.SetNextSpawnLocation(new Location(159.5f + ran, 129.0f + idk));
                }
            };
        }
    }
}