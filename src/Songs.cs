using Npgsql;

record Song(int songid, string title, int length, DateTime releasedate, int timeslistened);
record SearchSong(string title, decimal length, int timeslistened, string name);

class Songs
{
    private static Song readerToSong(NpgsqlDataReader reader)
    {
        return new Song((int)reader["songid"], (string)reader["title"], (int)reader["length"], (DateTime)reader["releasedate"], (int)reader["timeslistened"]);
    }

    private static SearchSong searchReaderToSong(NpgsqlDataReader reader) {
        return new SearchSong((string)reader["title"], (decimal)reader["length"], (int)reader["timeslistened"], (string)reader["name"]);
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
            var cmd = new NpgsqlCommand($"SELECT title, length, timeslistened, a2.name FROM song LEFT JOIN artistsong a on song.songid = a.songid LEFT JOIN artist a2 on a.artistid = a2.artistid WHERE UPPER(song.title) LIKE UPPER('%{title}_') AND a2.name IS NOT NULL;", database);
            var reader = cmd.ExecuteReader();
            var returnSongs = new List<SearchSong>();

            while (reader.Read()) {
                Console.WriteLine(reader);
                returnSongs.Add(searchReaderToSong(reader));
            }
            reader.Close();
            return returnSongs;
        }
        else {
            return null;
        }
    }
}