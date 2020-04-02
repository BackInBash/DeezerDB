using DeezerDB.Models.AllPlaylists;
using DeezerDB.Models.DeezerPlaylist;
using DeezerDB.Models.DeezerTrack;
using DeezerDB.Models.UserDataModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DeezerDB
{
    class API
    {
        public API()
        {
            GetDeezerAPILogin().Wait();
        }

        public API(string secret)
        {
            API.secret = secret;
            GetDeezerAPILogin().GetAwaiter().GetResult();
        }

        // User Agent: Chrome Version 80.0.3987.116
        private readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.116 Safari/537.36";
        private readonly string apiurl = "https://www.deezer.com/ajax/gw-light.php";
        private readonly string actionurl = "https://www.deezer.com/ajax/action.php";
        private readonly string official = "https://api.deezer.com/";
        private readonly string api_version = "api_version=1.0";
        private readonly string api_input = "input=3";
        private readonly string api_token = "api_token=";
        private readonly string method = "method=";
        private readonly string cid = "cid=";

        internal string userid;
        private string apiKey;
        private string csrfsid;
        public static string secret = "";


        internal async Task<string> Requestasync(string Dmethod, string payload = null)
        {

            var request = (dynamic)null;
            if (string.IsNullOrEmpty(apiKey))
            {
                request = (WebRequest)WebRequest.Create(apiurl + "?" + api_version + "&" + api_token + "&" + api_input + "&" + method + Dmethod + "&" + cid + GenCid());
            }
            else
            {
                request = (WebRequest)WebRequest.Create(apiurl + "?" + api_version + "&" + apiKey + "&" + api_input + "&" + method + Dmethod + "&" + cid + GenCid());
            }

            request.Method = "POST";

            if (!string.IsNullOrEmpty(payload))
            {
                request.ContentType = "application/json; charset=utf-8";
            }

            request.UserAgent = this.UserAgent;
            request.Headers["User-Agent"] = this.UserAgent;
            request.Headers["Cache-Control"] = "max-age=0";
            request.Headers["accept-language"] = "en-US,en;q=0.9,en-US;q=0.8,en;q=0.7";
            request.Headers["accept-charset"] = "utf-8,ISO-8859-1;q=0.8,*;q=0.7";
            request.Headers["cookie"] = "arl=" + secret + "; sid=" + csrfsid;

            var content = string.Empty;
            request.CookieContainer = new CookieContainer();

            try
            {
                if (!string.IsNullOrEmpty(payload))
                {
                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        streamWriter.Write(payload);
                    }
                }

                using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                {
                    if (string.IsNullOrEmpty(csrfsid))
                    {
                        foreach (Cookie cook in response.Cookies)
                        {
                            if (cook.Name.Equals("sid"))
                            {
                                csrfsid = cook.Value;
                            }
                        }
                    }
                    using (var stream = response.GetResponseStream())
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            content = sr.ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException we)
            {
                // Wait before the next request
                Thread.Sleep(1000);
                // Converting Data
                int errorCode = (int)((HttpWebResponse)we.Response).StatusCode;
                int startsWith = 5;
                // Retry HTTP Request on HTTPError 5xx
                if (errorCode.ToString().StartsWith(startsWith.ToString()))
                {
                    throw new WebException("ERROR during HTTP Request: " + we.Message + " Error Code: " + (int)((HttpWebResponse)we.Response).StatusCode);
                }
                else
                {
                    throw new WebException("ERROR during HTTP Request: " + we.Message + " Error Code: " + (int)((HttpWebResponse)we.Response).StatusCode);
                }
            }
            return content;
        }

        private int GenCid()
        {
            Random rnd = new Random();
            return rnd.Next(100000000, 999999999);
        }

        /// <summary>
        /// Get Deezer API Creds
        /// </summary>
        /// <returns></returns>
        private async Task GetDeezerAPILogin()
        {
            string webresult = await Requestasync("deezer.getUserData");
            var welcome = (dynamic)null;
            try
            {
                welcome = UserDataModel.FromJson(webresult);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            // Check for Valid User ID
            if (welcome.Results.User.UserId > 0)
            {
                // Check for Valid API Key
                if (!string.IsNullOrEmpty(welcome.Results.CheckForm) && welcome.Results.User.UserId != 0)
                {
                    // Write API Key to Var
                    apiKey = api_token + welcome.Results.CheckForm;
                    // Write UserID to Var
                    userid = welcome.Results.User.UserId.ToString();
                }
                else
                {
                    throw new Exception("Wrong User Information");
                }
            }
            else
            {
                throw new Exception("Cannot get Deezer API Key");
            }
        }


        /// <summary>
        /// Get a List of all Playlists
        /// </summary>
        /// <returns></returns>
        public partial class RequestAllPlaylists
        {
            public string user_id;
            public string tab;
            public long nb;
        }
        public async Task<List<long>> GetAllPlaylistsasync()
        {

            RequestAllPlaylists playlists = new RequestAllPlaylists()
            {
                nb = 40,
                tab = "playlists",
                user_id = userid
            };

            string json = JsonConvert.SerializeObject(playlists, Formatting.None);

            string jsonresult = await Requestasync("deezer.pageProfile", json);

            var result = (dynamic)null;

            try
            {
                result = AllPlaylists.Request.FromJson(jsonresult);
            }
            catch (JsonSerializationException ex)
            {
                try
                {
                    result = JsonConvert.DeserializeObject<dynamic>(jsonresult);
                    throw new Exception("ERROR: " + result.error.VALID_TOKEN_REQUIRED);
                }
                catch (Exception)
                {
                    throw new Exception(ex.Message);
                }
            }

            List<long> playlist = new List<long>();

            foreach (var i in result.Results.Tab.Playlists.Data)
            {
                playlist.Add(long.Parse(i.PlaylistId));
            }

            return playlist;
        }

        /// <summary>
        /// Get Deezer Playlist from Official API
        /// </summary>
        /// <param name="id">Playlist ID</param>
        /// <returns></returns>
        public async Task<Playlist> GetPlaylist(long id)
        {
            WebClient web = new WebClient();
            Uri uri = new Uri(official + "playlist/" + id);
            string json = await web.DownloadStringTaskAsync(uri);
            return JsonConvert.DeserializeObject<Playlist>(json);
        }

        /// <summary>
        /// Get Deezer Track
        /// </summary>
        /// <param name="id">Playlist ID</param>
        /// <returns></returns>
        public List<Track> GetTracks(Playlist playlist)
        {
            string tracks = JsonConvert.SerializeObject(playlist.Tracks.Data);
            var _tracks = JsonConvert.DeserializeObject<IEnumerable<Track>>(tracks);

            List<Track> track = new List<Track>(_tracks);
            return track;
        }
    }
}
