using Npgsql;

record Song(int songid, string title, decimal length, DateTime releasedate, int timeslistened);
record SongIDs(int songid);

class Songs
{
    public static Song readerToSong(NpgsqlDataReader reader)
    {
        return new Song(
                (int)reader["songid"],
                (string)reader["title"],
                (decimal)reader["length"],
                (DateTime)reader["releasedate"],
                (int)reader["timeslistened"]
        );
    }

    public static SongIDs songIDReaderToSongID(NpgsqlDataReader reader)
    {
        return new SongIDs((int)reader["songid"]);
    }
    
    private static void PrinSongCommands()
    {
        Console.WriteLine("===============Songs==============");
        Console.WriteLine("Listen to a Song:    listen");
        Console.WriteLine("Show this Menu:      help");
        Console.WriteLine("Back to Home:        back");
    }


    public static void HandleInput(NpgsqlConnection database)
    {
        PrinSongCommands();
        for (;;)
        {
            var input = Util.GetInput(Util.GetServerPrompt("/songs"));
            var inputArgs = input.Split(" ");
            switch (inputArgs[0].ToLower())
            {
                // listen to a song
                case "listen":
                    ListenInput(database);
                    break;
                
                // show help menu
                case "help":
                    PrinSongCommands();
                    break;
                
                // return home
                case "exit":
                case "back": 
                    return;
            }
        }
    }

    public static List<Song> GetSongs(NpgsqlConnection database)
    {
        var cmd = new NpgsqlCommand("SELECT * FROM song", database);
        var reader = cmd.ExecuteReader();
        List<Song> songs = new List<Song>();
        while (reader.Read())
        {
            songs.Add(readerToSong(reader));
        }

        reader.Close();
        return songs;
    }

    public static string FormatSong(NpgsqlConnection database, Song song)
    {
        var artists = string.Join(", ", Artists.ForSong(database, song.songid).Select(a => a.name));
        if (artists.Length > 0) artists = " | by " + artists;
        var album = Albums.ForSong(database, song.songid);
        string albumstr = album == null ? "" : " | in " + album.name;
        int? ulistens = Listens.UserCountForSong(database, song.songid);
        string userListens = ulistens == null ? "" : $" ({ulistens})";
        return $"{song.title}{artists}{albumstr}"
            + $" | {song.length / 60} minutes"
            + $" | released {song.releasedate.ToString("MM/dd/ yyyy")}"
            + $" | {song.timeslistened}{userListens} listens";
    }

    public static List<Song> GetSongs(NpgsqlConnection database, List<int> ids)
    {
        List<Song> songs = new List<Song>();
        if (ids.Count == 0) return songs;
        var cmd = new NpgsqlCommand($"SELECT * FROM song WHERE songid in ({string.Join(",", ids)})", database);
        var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            songs.Add(readerToSong(reader));
        }

        reader.Close();
        return songs;
    }

    private static Song? QuerySong(NpgsqlConnection database, string songTitle)
    {

        var cmd = new NpgsqlCommand($"SELECT * FROM song WHERE title='{songTitle}'", database);
        var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            Song song = readerToSong(reader);
            reader.Close();
            return song;
        }

        reader.Close();
        return null;
    }

    public static Song? SelectSong(NpgsqlConnection database)
    {
        Song? song = null;

        while (song == null)
        {
            var title = Util.GetInput("Enter song name: ");
            if (title == "back") return null;
            song = QuerySong(database, title);

            if (song == null)
            {
                Console.WriteLine("Unknown song title, try again or back");
            }
        }

        return song;
    }

    public static void PrintSongs(NpgsqlConnection database, List<Song> songs)
    {
        foreach (var song in songs)
        {
            Console.WriteLine($"    {Songs.FormatSong(database, song)}");
        }
    }

    private static void ListenInput(NpgsqlConnection database, string? songName=null)
    {
        songName ??= Util.GetInput("Song Name: ");  // assign name if none given
        Song? song = QuerySong(database, songName);

        if (song == null)
        {
            Util.ServerMessage($"Couldn't find song \"{songName}\"");
            return;
        }

        ListenTo(database, song);
    }

    public static void ListenTo(NpgsqlConnection database, Song song)
    {
        // update the global stats
        var query = new NpgsqlCommand($"UPDATE song SET timeslistened = timeslistened + 1 WHERE title = '{song.title}'", database);
        query.Prepare();
        query.ExecuteNonQuery();

        var attemptUpdateQuery = new NpgsqlCommand($"UPDATE listen SET count = count + 1 WHERE userid = {Users.LoggedInUser.userid} AND songid = {song.songid}", database);
        attemptUpdateQuery.Prepare();

        if (attemptUpdateQuery.ExecuteNonQuery() == 0)
        {
            var insertOrUpdate = new NpgsqlCommand($"INSERT INTO listen(userid, songid, count) VALUES({Users.LoggedInUser.userid}, {song.songid}, 1)", database);
            insertOrUpdate.Prepare();
            insertOrUpdate.ExecuteNonQuery();
        }
    }
}
