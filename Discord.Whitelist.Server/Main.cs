using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

namespace Discord.Whitelist.Server
{
    public class Main : BaseScript
    {
        public Main()
        {
            Debug.WriteLine("Discord Whitelist Starting!");
        }

        [EventHandler("onServerResourceStart")]
        private void OnServerResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName) return;

            WhitelistConfig.Load();
        }

        [EventHandler("playerConnecting")]
        private async void OnPlayerConnecting([FromSource] Player player, string playerName, dynamic setKickReason, dynamic deferrals)
        {
            try
            {
                await Delay(0);
                deferrals.defer();

                string DiscordID = player.Identifiers["discord"];

                deferrals.update($"Checking for Discord ID...");

                if (string.IsNullOrEmpty(DiscordID)) deferrals.done($"You must have Discord open (and Linked) in order to connect to the server!");

                // ---------------- Allows us to access the Discord API - Without this HTTP Client Handler the API will error. ---------------- //
                HttpClientHandler httpClientHandler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Automatic,
                    UseProxy = true,
                    UseDefaultCredentials = true
                };
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                // ---------------------------------------------------------------------------------------------------------------------------- //

                using (var DiscordAPI = new HttpClient(httpClientHandler))
                {
                    DiscordAPI.DefaultRequestHeaders.Accept.Clear();
                    DiscordAPI.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    DiscordAPI.DefaultRequestHeaders.Add("Authorization", $"Bot {Config.WhitelistConfig.BOTTOKEN}");

                    HttpResponseMessage memberObjRes = DiscordAPI.GetAsync($"https://discord.com/api/v9/guilds/{Config.WhitelistConfig.GUILDID}/members/{DiscordID}").GetAwaiter().GetResult();

                    if (!memberObjRes.IsSuccessStatusCode)
                    {
                        if (memberObjRes.StatusCode == HttpStatusCode.NotFound)
                        {
                            Debug.WriteLine($"------------------------------------------------------------------------------------------------------\n{playerName} ({DiscordID}) was unable to connect to the server.\nReason: Not in the Discord Server.\n------------------------------------------------------------------------------------------------------");
                            deferrals.presentCard("{\"type\":\"AdaptiveCard\",\"version\":\"1.5\",\"actions\":[{\"type\":\"Action.OpenUrl\",\"title\":\"Join The Discord Server\",\"style\":\"positive\",\"url\":\"" + Config.WhitelistConfig.DISCORDINV + "\"}],\"body\":[{\"type\":\"TextBlock\",\"text\":\"You must be in our discord server (and whitelisted) in order to connect to our server(s)!\",\"wrap\":true}]}");
                            return;
                        }
                        else
                        {
                            deferrals.done($"Something went wrong while connecting to the server!");
                            var memberErrObjStr = await memberObjRes.Content.ReadAsStringAsync();
                            Debug.WriteLine($"Error {memberObjRes.StatusCode}: {memberErrObjStr}");
                        }
                    }

                    var memberObjStr = await memberObjRes.Content.ReadAsStringAsync();

                    GuildMember memberObj = JsonConvert.DeserializeObject<GuildMember>(memberObjStr);

                    bool successWhitelist = false;

                    foreach (string roleID in Config.WhitelistConfig.ROLEIDS)
                    {
                        if (memberObj.Roles.Contains(roleID))
                        {
                            successWhitelist = true;
                            Debug.WriteLine($"------------------------------------------------------------------------------------------------------\n{playerName} ({DiscordID}) connected successfully to the server!\n------------------------------------------------------------------------------------------------------");
                            deferrals.done();
                        }
                    }

                    if (successWhitelist == false)
                    {
                        deferrals.done($"You must be Whitelisted in order to connect to the server!");
                        Debug.WriteLine($"------------------------------------------------------------------------------------------------------\n{playerName} ({DiscordID}) was unable to connect to the server.\nReason: Not Whitelisted in the Discord Server.\n------------------------------------------------------------------------------------------------------");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                deferrals.done($"Something went wrong while connecting to the server!");
                Debug.WriteLine(ex.ToString());
            }
        }
    }

    #region Just a whole lot of class shit for Discord (Line 82 to be exact)
    public class User
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        [JsonProperty("avatar_decoration")]
        public string AvatarDecoration { get; set; }

        [JsonProperty("discriminator")]
        public string Discriminator { get; set; }

        [JsonProperty("public_flags")]
        public int PublicFlags { get; set; }
    }
    public class GuildMember
    {
        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        [JsonProperty("communication_disabled_until")]
        public DateTimeOffset? TimedOutUntil { get; set; }

        [JsonProperty("flags")]
        public int Flags { get; set; }

        [JsonProperty("is_pending")]
        public bool IsPending { get; set; }

        [JsonProperty("joined_at")]
        public DateTimeOffset JoinedAt { get; set; }

        [JsonProperty("nick")]
        public string Nick { get; set; }

        [JsonProperty("pending")]
        public bool Pending { get; set; }

        [JsonProperty("premium_since")]
        public DateTimeOffset? PremiumSince { get; set; }

        [JsonProperty("roles")]
        public List<string> Roles { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("mute")]
        public bool Mute { get; set; }

        [JsonProperty("deaf")]
        public bool Deaf { get; set; }
    }
    #endregion
}
