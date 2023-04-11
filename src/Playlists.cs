using Npgsql;

record Playlist(int playlistid, int userid, DateTime creationdate, string playlistname);

class Playlists
{
    private static void PrintPlaylistCommands()
    {
        Console.WriteLine("===========Playlists===========");
        Console.WriteLine("Create a playlist:       create");
        Console.WriteLine("List your playlists:     list");
        Console.WriteLine("View a playlist:         view");
        Console.WriteLine("Listen to a playlist:    listen");
        Console.WriteLine("Rename a playlist:       rename");
        Console.WriteLine("Edit a playlist:         edit");
        Console.WriteLine("Delete a playlist:       delete");
        Console.WriteLine("Show this Menu:          help");
        Console.WriteLine("Back to Home:            back");
    }
  

    public static void HandleInput(NpgsqlConnection database)
    {
        PrintPlaylistCommands();
        while (true)
        {
            var input = Util.GetInput(Util.GetServerPrompt("/playlists"));
            var inputArgs = input.Split(" ");
            switch (inputArgs[0].ToLower())
            {
                // Create new playlist
                case "create":
                    Create(database);
                    break;
                
                // List exising playlists
                case "list":
                    ListPlaylists(database);
                    break;
                
                // View a given playlist
                case "view":
                    Info(database);
                    break;
                
                // listen to a given playlist
                case "listen":
                    ListenTo(database);
                    break;
                
                // Rename a given playlist
                case "rename":
                    Rename(database);
                    break;
                
                // Edit a given playlist
                case "edit":
                    HandleEditInput(database);
                    break;
                
                // Delete a given playlist
                case "delete":
                    Delete(database);
                    break;
                
                // Print the help menu
                case "help":
                    PrintPlaylistCommands();
                    break;
                
                // return home
                case "exit":
                case "back":
                    return;
            }
        }
    }
    
    // Commands
    
    private static void Create(NpgsqlConnection database)
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
    
    private static void ListPlaylists(NpgsqlConnection database)
    {
        var playlists = GetForUser(database, Users.LoggedInUser!.userid);
        Console.WriteLine($"\nPlaylists: ");
        foreach (var playlist in playlists)
        {
            (var duration, var numSongs) = GetDurationAndNumSongs(database, playlist.playlistid);
            Console.WriteLine($"\tPlaylist: {playlist.playlistname}, Duration: {duration / 60} minutes, Number of Songs: {numSongs}");
        }
    }

    private static void Info(NpgsqlConnection database)
    {
        int? playlistid = GetPlaylist(database);
        if (playlistid == null) return;
        DisplayPlaylist(database, (int)playlistid);
    }
    
    private static void ListenTo(NpgsqlConnection database)
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
    
    private static void HandleEditInput(NpgsqlConnection database)
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
                    InsertSong(database, (int)playlistId);
                    break;
                case "remove":
                    RemoveSong(database, (int)playlistId);
                    break;
                case "add album":
                    InsertAlbum(database, (int)playlistId);
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
    
    // Utils
    private static Playlist readerToPlaylist(NpgsqlDataReader reader)
    {
        return new Playlist(
            (int)reader["playlistid"],
            (int)reader["userid"],
            (DateTime)reader["creationdate"],
            (string)reader["playlistname"]
        );
    }
    

    private static int? GetPlaylist(NpgsqlConnection database)
    {
        Playlists.ListPlaylists(database);
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

    
    private static void DisplayPlaylist(NpgsqlConnection database, int playlistid)
    {
        Console.WriteLine($"\nSongs in playlist: ");
        Songs.PrintSongs(database, GetSongs(database, playlistid));
    }

  

    private static List<Playlist> GetForUser(NpgsqlConnection database, int userid)
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

    private static List<Song> GetSongs(NpgsqlConnection database, int playlistid)
    {
        return Songs.GetSongs(database, GetSongIds(database, playlistid));
    }

    private static List<int> GetSongIds(NpgsqlConnection database, int playlistid)
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

    private static (float, int) GetDurationAndNumSongs(NpgsqlConnection database, int playlistId)
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

    private static void InsertSong(NpgsqlConnection database, int playlistid)
    {
        Song? song = Songs.SelectSong(database);
        if (song == null) return;

        var insert = new NpgsqlCommand($"INSERT INTO songplaylist(songid, playlistid) VALUES ({song.songid}, {playlistid})", database);
        insert.Prepare();
        insert.ExecuteNonQuery();
        Console.WriteLine($"Added {song.title} to playlist");
    }

    private static void RemoveSong(NpgsqlConnection database, int playlistid)
    {
        Song? song = Songs.SelectSong(database);
        if (song == null) return;

        var remove = new NpgsqlCommand($"DELETE FROM songplaylist WHERE playlistid={playlistid} AND songid={song.songid}", database);
        remove.Prepare();
        remove.ExecuteNonQuery();
        Console.WriteLine($"Removed {song.title} from playlist");
    }

    private static void InsertAlbum(NpgsqlConnection database, int playlistid)
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

   
}
