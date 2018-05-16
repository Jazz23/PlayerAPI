using Lib_K_Relay;
using Lib_K_Relay.Interface;
using Lib_K_Relay.Networking;
using Lib_K_Relay.Networking.Packets;
using Lib_K_Relay.Networking.Packets.Client;
using Lib_K_Relay.Networking.Packets.Server;
using Lib_K_Relay.Networking.Packets.DataObjects;
using Lib_K_Relay.Utilities;
using PlayerAPI;

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

        public void Initialize(Proxy proxy)
        {
            PlayerAPI.PlayerAPI.Start(proxy);
            proxy.HookCommand("fellow", (c, co, a) =>
            {
                if (c.Self().TargetEntity == null) c.FollowEntity(c.GetEntityByName(a[0]));
                else c.StopFollowingEntity();
            });
        }
    }
}