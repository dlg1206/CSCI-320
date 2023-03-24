using Npgsql;

record Playlist(int playlistid, int userid, DateTime creationdate, string playlistname);

class Playlists
{
    private static Playlist readerToPlaylist(NpgsqlDataReader reader)
    {
        return new Playlist(
                (int)reader["playlistid"],
                (int)reader["userid"],
                (DateTime)reader["creationdate"],
                (string)reader["playlistname"]
        );
    }

    public static void HandleInput(NpgsqlConnection database)
    {
        while (true)
        {
            Console.WriteLine("Playlist possibilities: back, create, list, rename, delete, edit");
            string? input = Console.ReadLine();
            switch (input)
            {
                case "create":
                    Playlists.Create(database);
                    break;
                case "list":
                    // this is guaranteed to not be null
                    Playlists.DisplayPlaylists(database, Users.LoggedInUser!.userid);
                    break;
                case "rename":
                    Playlists.Rename(database);
                    break;
                case "delete":
                    Playlists.Delete(database);
                    break;
                case "edit":
                    Playlists.HandleEditInput(database);
                    break;
                case "back":
                    return;
                default:
                    Console.WriteLine("Unknown command, try again");
                    break;
            }
        }
    }

    public static void HandleEditInput(NpgsqlConnection database)
    {
        Playlists.DisplayPlaylists(database, Users.LoggedInUser!.userid);
        Console.WriteLine("Enter the playlist to edit");
        int? playlistId = null;
        while (!playlistId.HasValue) {
            var playlistName = Console.ReadLine();
            if (playlistName == "back") {
                return;
            }
            List<Playlist> playlists = GetForUser(database, Users.LoggedInUser!.userid);
            playlistId = playlists.Find(x => x.playlistname == playlistName)?.playlistid;
            if (playlistId == null) {
                Console.WriteLine("Couldn't find that playlist, try again or back");
            }
        }


        while (true) {
            DisplayPlaylist(database, (int)playlistId);
            Console.WriteLine("Playlist edit possibilities: back, add, remove");
            string? input = Console.ReadLine();
            switch (input) {
                case "add":
                    Playlists.InsertSong(database, (int)playlistId);
                    break;
                case "remove":
                    Playlists.RemoveSong(database, (int)playlistId);
                    break;
                case "back":
                    return;
                default:
                    Console.WriteLine("Unknown command, try again");
                    break;
            }
        }
    }

    private static void Delete(NpgsqlConnection database)
    {
        Console.WriteLine("Enter the playlist to delete");
        var playlistName = Console.ReadLine();

        List<Playlist> playlists = GetForUser(database, Users.LoggedInUser!.userid);
        var playlistId = playlists.Find(x => x.playlistname == playlistName)?.playlistid;

        if (playlistId != null)
        {
            var deleteSongPlaylistReference = new NpgsqlCommand($"DELETE FROM songplaylist WHERE playlistid = {playlistId}", database);
            deleteSongPlaylistReference.Prepare();
            deleteSongPlaylistReference.ExecuteNonQuery();
        }

        var delete = new NpgsqlCommand($"DELETE FROM playlist WHERE playlistname = '{playlistName}' AND userid = {Users.LoggedInUser!.userid}", database);
        delete.Prepare();
        delete.ExecuteNonQuery();
    }

    private static void Rename(NpgsqlConnection database)
    {
        Console.WriteLine("Enter the playlist to rename");
        var playlistName = Console.ReadLine();
        Console.WriteLine("Enter the new playlist name");
        var newPlaylistName = Console.ReadLine();

        var update = new NpgsqlCommand($"UPDATE playlist SET playlistname = '{newPlaylistName}' WHERE playlistname = '{playlistName}' AND userid = {Users.LoggedInUser!.userid}", database);
        update.Prepare();
        update.ExecuteNonQuery();
    }

    public static void DisplayPlaylists(NpgsqlConnection database, int userid)
    {
        List<Playlist> playlists = GetForUser(database, userid);
        Console.WriteLine($"\nPlaylists: ");
        foreach (var playlist in playlists)
        {
            (float duration, int numSongs) = GetDurationAndNumSongs(database, playlist.playlistid);
            Console.WriteLine($"    Playlist: {playlist.playlistname}, Duration: {duration} seconds, Number of Songs: {numSongs}");
        }
    }

    public static void DisplayPlaylist(NpgsqlConnection database, int playlistid)
    {
        Console.WriteLine($"\nSongs in playlist: ");
        foreach (var song in GetSongs(database, playlistid))
        {
            Console.WriteLine($"    {Songs.FormatSong(database, song)}");
        }
    }

    public static void Create(NpgsqlConnection database)
    {
        Console.WriteLine("Enter playlist name");
        var playlistName = Console.ReadLine();

        var insert = new NpgsqlCommand("INSERT INTO playlist(userid, creationdate, playlistname) VALUES ($1, $2, $3)", database)
        {
            Parameters = {
                new() { Value = Users.LoggedInUser.userid },
                new() { Value = DateTime.Now },
                new() { Value = playlistName }
            }
        };

        insert.Prepare();
        insert.ExecuteNonQuery();
    }

    public static List<Playlist> GetForUser(NpgsqlConnection database, int userid)
    {
        var query = new NpgsqlCommand($"SELECT * FROM playlist WHERE userid={userid} ORDER BY playlistname ASC", database);
        var reader = query.ExecuteReader();

        List<Playlist> playlists = new List<Playlist>();
        while (reader.Read())
        {
            playlists.Add(readerToPlaylist(reader));
        }

        reader.Close();

        return playlists;
    }

    public static List<Song> GetSongs(NpgsqlConnection database, int playlistid)
    {
        var query = new NpgsqlCommand($"SELECT songid FROM songplaylist WHERE playlistid={playlistid}", database);
        var reader = query.ExecuteReader();

        List<int> ids = new List<int>();
        while (reader.Read())
        {
            ids.Add((int)reader["songid"]);
        }
        reader.Close();

        return Songs.GetSongs(database, ids);
    }

    public static (float, int) GetDurationAndNumSongs(NpgsqlConnection database, int playlistId)
    {
        var query = new NpgsqlCommand($"SELECT SUM(length), COUNT(*) FROM (SELECT * FROM song, songplaylist WHERE playlistid = {playlistId} AND song.songid = songplaylist.songid) as songs", database);
        var reader = query.ExecuteReader();
        while (reader.Read())
        {
            float sum;
            try
            {
                sum = reader.GetFloat(0);
            }
            catch (InvalidCastException)
            {
                sum = 0;
            }
            var numSongs = reader.GetInt32(1);
            reader.Close();
            return (sum, numSongs);
        }

        reader.Close();
        return (-1, 0);
    }

    public static void InsertSong(NpgsqlConnection database, int playlistid)
    {
        Song? song = Songs.SelectSong(database);
        if (song == null) return;

        var insert = new NpgsqlCommand($"INSERT INTO songplaylist(songid, playlistid) VALUES ({song.songid}, {playlistid})", database);
        insert.Prepare();
        insert.ExecuteNonQuery();
        Console.WriteLine($"Added {song.title} to playlist");
    }

    public static void RemoveSong(NpgsqlConnection database, int playlistid)
    {
        Song? song = Songs.SelectSong(database);
        if (song == null) return;

        var remove = new NpgsqlCommand($"DELETE FROM songplaylist WHERE playlistid={playlistid} AND songid={song.songid}", database);
        remove.Prepare();
        remove.ExecuteNonQuery();
        Console.WriteLine($"Removed {song.title} from playlist");
    }
}
