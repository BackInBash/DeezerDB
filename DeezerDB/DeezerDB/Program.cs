using System;

namespace DeezerDB
{
    class Program
    {
        private static string arl = string.Empty;
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine("Usage: DeezerDB.exe -arl {TOKEN}");
                Environment.Exit(0);
            }

            for(int i = 0; i<args.Length; i++)
            {
                if (args[i].Equals("-arl"))
                {
                    arl = args[i + 1];
                }
            }

            if (string.IsNullOrEmpty(arl))
            {
                Console.WriteLine("ERROR: No ARL Cookie Provided");
                Environment.Exit(1);
            }

            // Get Deezer Playlist IDs
            API api = new API(arl);
            ElasticSearch elastic = new ElasticSearch();
            var ids = await api.GetAllPlaylistsasync();

            foreach(long id in ids)
            {
                var playlist = await api.GetPlaylist(id);
                elastic.CreatePlaylistEntity(playlist);
                var tracks = api.GetTracks(playlist);
                foreach (var track in tracks)
                {
                    track.inPlaylist = playlist.Title;
                    elastic.CreateTrackEntity(track);
                }
            }
        }
    }
}
