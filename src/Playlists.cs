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
            Console.WriteLine("Playlist possibilities: back, create, list, rename, delete, edit, view, listen");
            string? input = Console.ReadLine();
            switch (input)
            {
                case "create":
                    Playlists.Create(database);
                    break;
                case "list":
                    Playlists.DisplayPlaylists(database);
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
                case "listen":
                    Playlists.ListenTo(database);
                    break;
                case "view":
                    Playlists.Info(database);
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
        int? playlistId = GetPlaylist(database);
        if (playlistId == null) return;
        while (true)
        {
            DisplayPlaylist(database, (int)playlistId);
            Console.WriteLine("Playlist edit possibilities: back, add, remove, add album");
            string? input = Console.ReadLine();
            switch (input)
            {
                case "add":
                    Playlists.InsertSong(database, (int)playlistId);
                    break;
                case "remove":
                    Playlists.RemoveSong(database, (int)playlistId);
                    break;
                case "add album":
                    Playlists.InsertAlbum(database, (int)playlistId);
                    break;
                case "back":
                    return;
                default:
                    Console.WriteLine("Unknown command, try again");
                    break;
            }
        }
    }

    private static int? GetPlaylist(NpgsqlConnection database)
    {
        Playlists.DisplayPlaylists(database);
        Console.WriteLine("Enter a playlist name");
        while (true)
        {
            var playlistName = Console.ReadLine();
            if (playlistName == "back")
            {
                return null;
            }
            var cmd = new NpgsqlCommand($"SELECT playlistid FROM playlist WHERE playlistname='{playlistName}' AND userid={Users.LoggedInUser!.userid}", database);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var id = (int)reader["playlistId"];
                reader.Close();
                return id;
            }
            reader.Close();
            Console.WriteLine("Couldn't find that playlist, try again or back");
        }
    }

    private static void Info(NpgsqlConnection database)
    {
        int? playlistid = GetPlaylist(database);
        if (playlistid == null) return;
        DisplayPlaylist(database, (int)playlistid);
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

    public static void DisplayPlaylists(NpgsqlConnection database)
    {
        List<Playlist> playlists = GetForUser(database, Users.LoggedInUser!.userid);
        Console.WriteLine($"\nPlaylists: ");
        foreach (var playlist in playlists)
        {
            (float duration, int numSongs) = GetDurationAndNumSongs(database, playlist.playlistid);
            Console.WriteLine($"    Playlist: {playlist.playlistname}, Duration: {duration / 60} minutes, Number of Songs: {numSongs}");
        }
    }

    public static void DisplayPlaylist(NpgsqlConnection database, int playlistid)
    {
        Console.WriteLine($"\nSongs in playlist: ");
        Songs.PrintSongs(database, GetSongs(database, playlistid));
    }

    public static void Create(NpgsqlConnection database)
    {
        Console.WriteLine("Enter playlist name");
        var playlistName = Console.ReadLine();

        var insert = new NpgsqlCommand("INSERT INTO playlist(userid, creationdate, playlistname) VALUES ($1, $2, $3)", database)
        {
            Parameters = {
                new() { Value = Users.LoggedInUser?.userid },
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
        return Songs.GetSongs(database, GetSongIds(database, playlistid));
    }

    public static List<int> GetSongIds(NpgsqlConnection database, int playlistid)
    {
        var query = new NpgsqlCommand($"SELECT songid FROM songplaylist WHERE playlistid={playlistid}", database);
        var reader = query.ExecuteReader();

        List<int> ids = new List<int>();
        while (reader.Read())
        {
            ids.Add((int)reader["songid"]);
        }
        reader.Close();

        return ids;
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

    public static void InsertAlbum(NpgsqlConnection database, int playlistid)
    {
        Album? album = Albums.SelectAlbum(database);
        if (album == null) return;

        List<Song> songs = Albums.GetSongs(database, album.albumid);
        if (songs.Count == 0)
        {
            Console.WriteLine("No songs were added.");
            return;
        }
        foreach (var id in GetSongs(database, playlistid))
        {
            songs.Remove(id);
        }
        List<string> valueList = new List<string>();
        foreach (var song in songs)
        {
            valueList.Add($"({song.songid}, {playlistid})");
        }
        string values = string.Join(", ", valueList);
        var insert = new NpgsqlCommand($"INSERT INTO songplaylist(songid, playlistid) VALUES {values}", database);
        insert.Prepare();
        insert.ExecuteNonQuery();
        Console.WriteLine($"Added {album.name} to playlist, which contains the following new songs:");
        Songs.PrintSongs(database, songs);
    }

    public static void ListenTo(NpgsqlConnection database)
    {
        int? playlistid = GetPlaylist(database);
        if (playlistid == null) return;
        var songs = GetSongs(database, (int)playlistid);
        if (songs.Count == 0) return;

        foreach (Song song in songs)
        {
            Songs.ListenTo(database, song);
        }
    }
}
