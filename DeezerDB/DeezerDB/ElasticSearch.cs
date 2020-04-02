using DeezerDB.Models.DeezerPlaylist;
using DeezerDB.Models.DeezerTrack;
using Nest;
using System;
using System.Net;

namespace DeezerDB
{
    class ElasticSearch
    {
        ElasticClient client;

        public ElasticSearch()
        {
            var node = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(node);
            client = new ElasticClient(settings);
        }

        public void CreatePlaylistEntity(Playlist data)
        {
            WebClient http = new WebClient();
            http.Headers.Add("Content-Type", "application/json");
            string json = string.Empty;
            try
            {
                var response = client.Index(data, idx => idx.Index("deezer_playlists"));

                if (!response.IsValid)
                {
                    Console.WriteLine("Elastic Request Failed: " + response.DebugInformation);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.Message + " Payload: " + json);
            }
        }
        public void CreateTrackEntity(Track data)
        {
            WebClient http = new WebClient();
            http.Headers.Add("Content-Type", "application/json");
            string json = string.Empty;
            try
            {
                var response = client.Index(data, idx => idx.Index("deezer_tracks"));

                if (!response.IsValid)
                {
                    Console.WriteLine("Elastic Request Failed: " + response.DebugInformation);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.Message + " Payload: " + json);
            }
        }
    }
}
