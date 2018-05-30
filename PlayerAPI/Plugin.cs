using Lib_K_Relay;
using Lib_K_Relay.Interface;

namespace PlayerAPI
{
    public class Plugin : IPlugin
    {
        public string GetAuthor()
        {
            return "Jazz";
        }

        public string GetName()
        {
            return "PlayerAPI";
        }

        public string GetDescription()
        {
            return "Loads of crap for coders, lots of plugins prolly use this so leave it here.";
        }

        public string[] GetCommands()
        {
            return new string[]
            {
                "playerapi"
            };
        }

        public void Initialize(Proxy proxy)
        {
            PlayerAPI.Start(proxy);
        }
    }
}
