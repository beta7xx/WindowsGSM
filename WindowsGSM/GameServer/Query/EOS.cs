using Newtonsoft.Json;
using OpenGSQ.Protocols;
using OpenGSQ.Responses.Battlefield;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.Properties;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace WindowsGSM.GameServer.Query
{

    //TODO: https://github.com/DiscordGSM/GameServerMonitor/blob/main/discordgsm/protocols/theisleevrima.py
    public class EOS
    {
        //not sure if they are on gameserver base, maybe they need to be set by the plugi7
        //The Island
        public string EOS_ClientId = "";
        public string EOS_ClientSecret = "";
        public string EOS_DeploymentId = "";
        public string EOS_GrantType = "client_credentials";
        public string EOS_ExtAuthType = "";
        public string EOS_ExtAuthToken = "";

        public string EOS_AccessToken = "";


        private IPEndPoint _IPEndPoint;
        private int _timeout;

        public EOS() 
        { 
        
        }
        public EOS(string clientId, string clientSecret, string deploymentId, string grantType = "client_credentials", string extAuthType = "", string extAuthToken = "") 
        {
            EOS_ClientId=clientId;
            EOS_ClientSecret=clientSecret;
            EOS_DeploymentId=deploymentId;
            EOS_GrantType=grantType;
            EOS_ExtAuthType=extAuthType;
            EOS_ExtAuthToken=extAuthToken;
        }

        //not sure if that constructor is ever used, but i'll leave it for compatibility reasons
        public EOS(string address, int port, int timeout = 5)
        {
            SetAddressPort(address, port, timeout);
        }

        public void SetAddressPort(string address, int port, int timeout = 5)
        {
            string ip = address;
            
            if (string.IsNullOrWhiteSpace(ip) || ip == "0.0.0.0")
                ip = "127.0.0.1";

            _IPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _timeout = timeout * 1000;
        }

        /// <summary>Retrieves information about the server including, but not limited to: its name, the map currently being played, and the number of players.</summary>
        /// <returns>Returns (key, value)</returns>
        public async Task<Dictionary<string, string>> GetInfo()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(EOS_AccessToken))
                        EOS_AccessToken = await OpenGSQ.Protocols.EOS.GetAccessTokenAsync(EOS_ClientId, EOS_ClientSecret, EOS_DeploymentId,
                            EOS_GrantType, EOS_ExtAuthType, EOS_ExtAuthToken);

                    File.WriteAllText("DebugEosToken.txt", JsonConvert.SerializeObject(EOS_AccessToken));
                    // Store response's data
                    var keys = new Dictionary<string, string>();
                    byte[] data = null;

                    var eos = new OpenGSQ.Protocols.EOS(_IPEndPoint.Address.ToString(), _IPEndPoint.Port, EOS_DeploymentId, EOS_AccessToken);

                    File.WriteAllText("DebugEosObject.txt", JsonConvert.SerializeObject(eos));
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var info = await eos.GetInfo();
                    watch.Stop();
                    var ping = watch.ElapsedMilliseconds * 1000;

                    var attributes = info["attributes"] ;
                    var settings = info["settings"];

                    keys["Name"] = attributes.GetProperty("SERVERNAME_s").GetString() ?? null;
                    keys["Map"] = attributes.GetProperty("MAP_NAME_s").GetString() ?? null;
                    keys["Players"] = info["totalPlayers"].GetInt32().ToString() ?? null;
                    keys["MaxPlayers"] = settings.GetProperty("maxPublicPlayers").GetString() ?? null;
                    
                    //keys["Ping"] = ping.ToString();
                    //TODO: not sure what else is needed, and what EOS delivers
                    File.WriteAllText("DebugEosQueryData.json", JsonConvert.SerializeObject(info));
                    return keys.Count <= 0 ? null : keys;
                }
                catch (Exception ex) 
                {
                    File.WriteAllText("DebugEosException.json", JsonConvert.SerializeObject(ex));
                    return null;
                }
            });
        }

        /// <summary>Retrieves information about the players currently on the server.</summary>
        /// <returns>Returns (id, (name, score, timeConnected))</returns>
        public async Task<Dictionary<int, (string, long, TimeSpan)>> GetPlayer()
        {
            return null;
        }

        public async Task<string> GetPlayersAndMaxPlayers()
        {
            try
            {
                Dictionary<string, string> kv = await GetInfo();
                return kv["Players"] + '/' + kv["MaxPlayers"];
            }
            catch
            {
                return null;
            }
        }
    }
}
