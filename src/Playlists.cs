using Npgsql;

record Playlist(int playlistid, int userid, DateTime creationdate, string playlistname);

class Playlists
{
    private static void PrintPlaylistCommands()
    {
        Console.WriteLine("==============Playlists===============");
        Console.WriteLine("Create a playlist:       create <name>");
        Console.WriteLine("List your playlists:     list");
        Console.WriteLine("View a playlist:         view <name>");
        Console.WriteLine("Listen to a playlist:    listen <name>");
        Console.WriteLine("Rename a playlist:       rename");
        Console.WriteLine("Edit a playlist:         edit <name>");
        Console.WriteLine("Delete a playlist:       delete <name>");
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
                    // Use given name or get name if none given
                    var name = inputArgs.Length < 2 ? Util.GetInput("Playlist Name: ") : inputArgs[1];
                    Create(database, name);
                    HandleEditInput(database, name);    // launch editor
                    break;
                
                // List exising playlists
                case "list":
                    ListPlaylists(database);
                    break;
                
                // View a given playlist
                case "view":
                    if(inputArgs.Length < 2)
                        Info(database, inputArgs[1]);
                    else
                        Info(database);
                    
                    break;
                
                // listen to a given playlist
                case "listen":
                    if(inputArgs.Length < 2)
                        ListenTo(database, inputArgs[1]);
                    else
                        ListenTo(database);
                    break;
                
                // Rename a given playlist
                case "rename":
                    Rename(database);
                    break;
                
                // Edit a given playlist
                case "edit":
                    if(inputArgs.Length < 2)
                        HandleEditInput(database, inputArgs[1]);
                    else
                        HandleEditInput(database);
                    break;
                
                // Delete a given playlist
                case "delete":
                    if(inputArgs.Length < 2)
                        Delete(database, inputArgs[1]);
                    else
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
    
    /// <summary>
    /// Add new playlist to the database
    /// </summary>
    /// <param name="database">database to use</param>
    /// <param name="playlistName">name of the playlist</param>
    private static void Create(NpgsqlConnection database, string? playlistName=null)
    {
        playlistName ??= Util.GetInput("Playlist Name: ");  // assign name if none given
        
        // add to ddb
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
        Console.WriteLine("Playlists:");
        foreach (var playlist in playlists)
        {
            var (duration, numSongs) = GetDurationAndNumSongs(database, playlist.playlistid);
            Console.WriteLine($"\tPlaylist: {playlist.playlistname}, Duration: {duration / 60} minutes, Number of Songs: {numSongs}");
        }
    }

    private static void Info(NpgsqlConnection database, string? playlistName=null)
    {
        playlistName ??= Util.GetInput("Playlist Name: ");  // assign name if none given
        var playlistid = GetPlaylist(database, playlistName);
        if (playlistid == null)
        {
            Util.ServerMessage($"Couldn't find playlist \"{playlistName}");
            return;
        }
        DisplayPlaylist(database, (int) playlistid);
    }
    
    private static void ListenTo(NpgsqlConnection database, string? playlistName=null)
    {
        playlistName ??= Util.GetInput("Playlist Name: ");  // assign name if none given
        
        int? playlistid = GetPlaylist(database, playlistName);
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
  
        // Get names
        var playlistName = Util.GetInput("Target Playlist: ");
        var newPlaylistName = Util.GetInput("New Name: ");

        var update = new NpgsqlCommand($"UPDATE playlist SET playlistname = '{newPlaylistName}' WHERE playlistname = '{playlistName}' AND userid = {Users.LoggedInUser!.userid}", database);
        update.Prepare();
        update.ExecuteNonQuery();
    }
    
    private static void HandleEditInput(NpgsqlConnection database, string? playlistName=null)
    {
        playlistName ??= Util.GetInput("Playlist Name: ");  // assign name if none given
        
        var playlistId = GetPlaylist(database, playlistName);
        if (playlistId == null) return;
        while (true)
        {
            DisplayPlaylist(database, (int) playlistId);
            Console.WriteLine("Options: done, add, remove, add album");
            var input = Util.GetInput(Util.GetServerPrompt("[Playlist Builder]"));
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
                case "done":
                    Util.ServerMessage("Playlist Saved!");
                    return;
            }
        }
    }
    
    private static void Delete(NpgsqlConnection database, string? playlistName=null)
    {
        playlistName ??= Util.GetInput("Playlist Name: ");  // assign name if none given
        
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
    

    private static int? GetPlaylist(NpgsqlConnection database, string playlistName)
    {
        int? id = null;
        var cmd = new NpgsqlCommand($"SELECT playlistid FROM playlist WHERE playlistname='{playlistName}' AND userid={Users.LoggedInUser!.userid}", database);
        var reader = cmd.ExecuteReader();
        if (reader.Read())
            id = (int) reader["playlistId"];
        else
            Util.ServerMessage($"Couldn't find playlist {playlistName}");
        reader.Close();
        return id;
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
