using Npgsql;

record Song(int songid, string title, decimal length, DateTime releasedate, int timeslistened);
record SearchSong(string title, decimal length, int timeslistened, string name);
record SearchArtist(int artistid);
record SongIDs(int songid);

class Songs
{
    private static Song readerToSong(NpgsqlDataReader reader)
    {
        return new Song(
                (int)reader["songid"],
                (string)reader["title"],
                (decimal)reader["length"],
                (DateTime)reader["releasedate"],
                (int)reader["timeslistened"]
        );
    }

    private static SearchSong searchReaderToSong(NpgsqlDataReader reader) {
        return new SearchSong((string)reader["title"], (decimal)reader["length"], (int)reader["timeslistened"], (string)reader["name"]);
    }

    private static SearchArtist artistReaderToArtist(NpgsqlDataReader reader) {
        return new SearchArtist((int)reader["artistid"]);
    }

    private static SongIDs songIDReaderToSongID(NpgsqlDataReader reader) {
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

    public static List<SearchSong>? SearchSongByTitle(NpgsqlConnection database, String title) {
        // Make sure song name exists
        if (title.Length > 0) {
            // Get song like name
            var cmd = new NpgsqlCommand($"SELECT title, length, timeslistened, a2.name FROM song LEFT JOIN artistsong a on song.songid = a.songid LEFT JOIN artist a2 on a.artistid = a2.artistid WHERE UPPER(song.title) LIKE UPPER('{title}') AND a2.name IS NOT NULL;", database);
            var reader = cmd.ExecuteReader();
            var returnSongs = new List<SearchSong>();

            while (reader.Read()) {
                returnSongs.Add(searchReaderToSong(reader));
            }
            reader.Close();
            return returnSongs;
        }
        else {
            return null;
        }
    }

    public static List<SearchSong>? SearchSongByArtist(NpgsqlConnection database, String artistName) {

        if (artistName.Length > 0) {
            // Get artist ids first
            var cmd = new NpgsqlCommand($"SELECT artistid FROM artist WHERE name LIKE '{artistName}'", database);
            var reader = cmd.ExecuteReader();
            var artists = new List<SearchArtist>();

            while (reader.Read()) {
                artists.Add(artistReaderToArtist(reader));
            }
            reader.Close();

            // Get song IDs matching artist IDs
            var songIDs = new List<SongIDs>();
            foreach (var id in artists) {
                var cmd1 = new NpgsqlCommand($"SELECT songid FROM artistsong WHERE artistid = {id.artistid}", database);
                var reader1 = cmd1.ExecuteReader();
                while (reader1.Read()) {
                    songIDs.Add(songIDReaderToSongID(reader1));
                }
                reader1.Close();
            }
            
            // Get the rest of the song data matching the song IDs
            var returnSongs = new List<SearchSong>();
            foreach (var id in songIDs) {
                var cmd3 = new NpgsqlCommand($"SELECT s.title, s.length, s.timeslistened, a2.name FROM song s LEFT JOIN artistsong a on s.songid = a.songid LEFT JOIN artist a2 on a.artistid = a2.artistid WHERE s.songid = {id.songid} AND a2.name IS NOT NULL;", database);
                var reader3 = cmd3.ExecuteReader();

                while (reader3.Read()) {
                    returnSongs.Add(searchReaderToSong(reader3));
                }
                reader3.Close();
            }
            return returnSongs;
        }
        else {
            return null;
        }
    }

    public static List<SearchSong>? SearchSongByAlbum(NpgsqlConnection database, String albumName) {
        return null;
    }

    public static List<SearchSong>? SearchSongByGenre(NpgsqlConnection database, String genreName) {
        return null;
    }
    
    public static string FormatSong(NpgsqlConnection database, Song song) {
        var artists = string.Join(", ", Artists.ForSong(database, song.songid));
        if (artists.Length > 0) artists = " by " + artists;
        return $"{song.title}{artists}: {song.length} seconds, released on {song.releasedate}";
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

    public static Song? SelectSong(NpgsqlConnection database) {
        Song? song = null;
        Console.WriteLine("Enter song name:");

        while (song == null) {
            var title = Console.ReadLine();
            if (title == "back") return null;
            var cmd = new NpgsqlCommand($"SELECT * FROM song WHERE title='{title}'", database);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                song = readerToSong(reader);
            } else {
                Console.WriteLine("Unknown song title, try again or back");
            }
            reader.Close();
        }

        return song;
    }

    public static void PrintSongs(NpgsqlConnection database, List<Song> songs) {
        foreach (var song in songs) {
            Console.WriteLine($"    {Songs.FormatSong(database, song)}");
        }
    }

    private static void ListenInput(NpgsqlConnection database)
    {
        Console.WriteLine("Enter the song name to listen to");
        string? song = Console.ReadLine();
        ListenTo(database, song);
    }

    public static void ListenTo(NpgsqlConnection database, string songName)
    {
        var query = new NpgsqlCommand($"UPDATE song SET timeslistened = timeslistened + 1 WHERE title = '{songName}'", database);
        query.Prepare();
        query.ExecuteNonQuery();
    }
}
