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


    public static void HandleInput(NpgsqlConnection database)
    {
        Console.WriteLine("Song input possibilities: listen to");
        string? input = Console.ReadLine();
        if (input != null)
        {
            switch (input)
            {
                case "listen to":
                    ListenInput(database);
                    break;

                default:
                    Console.WriteLine("Not an input");
                    HandleInput(database);
                    break;
            }
        }
        else
        {
            Console.WriteLine("input is null, try again");
            HandleInput(database);
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
        return $"{song.title}{artists}{albumstr} | {song.length} seconds | released {song.releasedate.ToString("MM/dd/yyyy")} | {song.timeslistened} listens";
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
        Console.WriteLine("Enter song name:");

        while (song == null)
        {
            var title = Console.ReadLine();
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

    private static void ListenInput(NpgsqlConnection database)
    {
        Console.WriteLine("Enter the song name to listen to");
        string? songName = Console.ReadLine();
        Song? song = QuerySong(database, songName);

        if (song == null)
        {
            Console.WriteLine("Not a song");
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
