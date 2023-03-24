using Npgsql;

record Song(int songid, string title, int length, DateTime releasedate, int timeslistened);

class Songs
{
    private static Song readerToSong(NpgsqlDataReader reader)
    {
        return new Song((int)reader["songid"], (string)reader["title"], (int)reader["length"], (DateTime)reader["releasedate"], (int)reader["timeslistened"]);
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