using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using SharpConfig;

namespace Discord.Whitelist.Server
{
    public class Config : BaseScript
    {
        public static WhitelistConfig WhitelistConfig;
    }

    public struct WhitelistConfig
    {
        public string GUILDID;
        public string BOTTOKEN;
        public string[] ROLEIDS;
        public string DISCORDINV;

        public static void Load()
        {
            WhitelistConfig config = new WhitelistConfig()
            {
                GUILDID = "",
                BOTTOKEN = "",
                ROLEIDS = { },
                DISCORDINV = ""
            };

            var configFile = Configuration.LoadFromFile(string.Format("{0}/WhitelistCFG.ini", GetResourcePath(GetCurrentResourceName())));

            try
            {
                if (!string.IsNullOrWhiteSpace(configFile.ToString()))
                {
                    var whitelistConfigSection = configFile["WHITELIST"];
                    config.GUILDID = whitelistConfigSection["GUILDID"].StringValue;
                    config.BOTTOKEN = whitelistConfigSection["BOTTOKEN"].StringValue;
                    config.ROLEIDS = whitelistConfigSection["ROLEIDS"].StringValueArray;

                    Config.WhitelistConfig = config;
                    Debug.WriteLine("[Discord WL] SUCCESS - The Config has been setup succesfully!");
                }
                else
                {
                    Debug.WriteLine("[Discord WL] ERROR - The Config file is empty, Please make sure the Config is setup correctly!");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Discord WL] ERROR - Something went wrong while loading the Config file.\n{ex}");
            }
        }
    }
}
