using Npgsql;

record Song(int songid, string title, decimal length, DateTime releasedate, int timeslistened);

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

    public static string FormatSong(NpgsqlConnection database, Song song) {
        var artists = string.Join(", ", Artists.ForSong(database, song.songid));
        if (artists.Length > 0) artists = " by " + artists;
        return $"{song.title}{artists}: {song.length} seconds, released on {song.releasedate}";
    }

    public static List<Song> GetSongs(NpgsqlConnection database, List<int> ids)
    {
        var cmd = new NpgsqlCommand($"SELECT * FROM song WHERE songid in ({string.Join(",", ids)})", database);
        var reader = cmd.ExecuteReader();
        List<Song> songs = new List<Song>();
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
}
