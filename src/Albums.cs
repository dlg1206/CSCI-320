using Npgsql;

record Album(int albumid, DateTime releaseDate, string name);

class Albums {
    private static Album ReaderToAlbum(NpgsqlDataReader reader) {
        return new Album(
                (int)reader["albumid"],
                (DateTime)reader["releasedate"],
                (string)reader["name"]
        );
    }

    public static Album? SelectAlbum(NpgsqlConnection database) {
        Album? album = null;
        Console.WriteLine("Enter album name:");

        while (album == null) {
            var title = Console.ReadLine();
            if (title == "back") return null;
            var cmd = new NpgsqlCommand($"SELECT * FROM album WHERE name='{title}'", database);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                album = ReaderToAlbum(reader);
            } else {
                Console.WriteLine("Unknown album name, try again or back");
            }
            reader.Close();
        }

        return album;
    }

    public static List<Song> GetSongs(NpgsqlConnection database, int albumid)
    {
        var query = new NpgsqlCommand($"SELECT songid FROM songalbum WHERE albumid={albumid}", database);
        var reader = query.ExecuteReader();

        List<int> ids = new List<int>();
        while (reader.Read())
        {
            ids.Add((int)reader["songid"]);
        }
        reader.Close();

        return Songs.GetSongs(database, ids);
    }
}
