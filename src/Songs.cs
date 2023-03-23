using Npgsql;

record Song(int songid, string title, int length, DateTime releasedate, int timeslistened);

class Songs
{
    private static Song readerToSong(NpgsqlDataReader reader)
    {
        return new Song((int)reader["songid"], (string)reader["title"], (int)reader["length"], (DateTime)reader["releasedate"], (int)reader["timeslistened"]);
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
}