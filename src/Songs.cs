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
    
    private static void PrintSongCommands()
    {
        Console.WriteLine("============Songs===========");
        Console.WriteLine("Listen to a Song:     listen");
        Console.WriteLine("Last Month's top 50:  popular");
        Console.WriteLine("Month's Top 5 Genres: genres");
        Console.WriteLine("For You:              recommended");
        Console.WriteLine("Show this Menu:       help");
        Console.WriteLine("Back to Home:         back");
    }


    public static void HandleInput(NpgsqlConnection database)
    {
        PrintSongCommands();
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
                
                case "popular":
                    ListTopFifty(database);
                    break;
                case "genres":
                    Rankings.ListTopFiveGenres(database);
                    break;
                case "recommended":
                    Rankings.ForYou(database);
                    break;
                
                // show help menu
                case "help":
                    PrintSongCommands();
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

    /// <summary>
    /// List the top 50 played songs in the past 3 days
    /// </summary>
    /// <param name="database">db to query</param>
    private static void ListTopFifty(NpgsqlConnection database)
    {
        // Get the top 50 most played songs in the last 30 days
        var cmd = new NpgsqlCommand(
            "SELECT title, count(timestamp) FROM ( SELECT songid, timestamp FROM listen WHERE listen.timestamp > now() - interval '30 day' group by songid, timestamp ) as topSongsPast30Days INNER JOIN ( SELECT songid, title FROM song ) as songNames ON topSongsPast30Days.songid = songNames.songid GROUP BY title ORDER BY count(timestamp) desc LIMIT 50;",
            database
            );
        var reader = cmd.ExecuteReader();
        
        // Formatting temp vars
        var songCount = 1;
        var longestLine = 0;
        var songTitles = new List<string>();
        var songPlays = new List<string>();
        // Read from the db into tmp vars
        while (reader.Read())
        {
            var line = $"Song {songCount++}: {(string) reader["title"]}\t";

            // update longest line length if needed
            if (line.Length > longestLine) longestLine = line.Length;

            songTitles.Add(line);
            songPlays.Add($"| {(long) reader["count"]} Play" + ((long) reader["count"] == 1 ? "" : "s"));
        }
        reader.Close();
        
        // Pretty print all songs and times played
        const string banner = "-= Top 50 Songs in the Past 30 Days =-";
        Console.WriteLine($"{Util.Tabs((longestLine - banner.Length) / 8)}{banner}");
        for (var i = 0; i < songTitles.Count; i++)
        {
            var line = songTitles[i];
            Console.WriteLine($"{line}{Util.Tabs((longestLine - line.Length) / 8)}{songPlays[i]}");
        }
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
            + $" | {Math.Round(song.length / 60, 2)} minutes"
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
            Console.WriteLine($"\t{FormatSong(database, song)}");
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
