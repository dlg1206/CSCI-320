using Npgsql;

record Album(int albumid, DateTime releaseDate, string name);

class Albums
{

    public static Album readerToAlbum(NpgsqlDataReader reader)
    {
        return new Album(
                (int)reader["albumid"],
                (DateTime)reader["releasedate"],
                (string)reader["name"]
        );
    }

    public static Album? SelectAlbum(NpgsqlConnection database)
    {
        Album? album = null;
        Console.WriteLine("Enter album name:");

        while (album == null)
        {
            var title = Console.ReadLine();
            if (title == "back") return null;
            var cmd = new NpgsqlCommand($"SELECT * FROM album WHERE name='{title}'", database);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                album = readerToAlbum(reader);
            }
            else
            {
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

    public static Album? ForSong(NpgsqlConnection database, int songid)
    {
        var query = new NpgsqlCommand($"SELECT a.* FROM album a join songalbum s on a.albumid=s.albumid WHERE s.songid={songid}", database);
        var reader = query.ExecuteReader();

        Album? album = null;

        if (reader.Read())
        {
            album = readerToAlbum(reader);
        }
        reader.Close();
        return album;
    }

    public static void HandleInput(NpgsqlConnection database)
    {
        Console.WriteLine("Album input possibilities: listen to");
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

    public static Album? GetAlbum(NpgsqlConnection database, string albumName)
    {
        var query = new NpgsqlCommand($"SELECT * FROM album WHERE albumname = {albumName}", database);
        var reader = query.ExecuteReader();
        if (reader.Read())
        {
            var album = readerToAlbum(reader);
            reader.Close();
            return album;
        }

        reader.Close();
        return null;
    }

    public static Album? GetAlbumForSong(NpgsqlConnection database, int songId)
    {
        var albumQuery = new NpgsqlCommand($"SELECT * FROM album WHERE albumid IN (SELECT albumid FROM songalbum WHERE songid = {songId})", database);
        var reader = albumQuery.ExecuteReader();
        if (reader.Read())
        {
            var album = readerToAlbum(reader);
            reader.Close();
            return album;
        }

        reader.Close();

        return null;
    }

    private static void ListenInput(NpgsqlConnection database)
    {
        Console.WriteLine("Enter the song name to listen to");
        string? album = Console.ReadLine();
        ListenTo(database, album);
    }

    public static void ListenTo(NpgsqlConnection database, string albumName)
    {
        Album? album = GetAlbum(database, albumName);
        if (album != null)
        {
            var updateQuery = new NpgsqlCommand($"UPDATE song SET timeslistened = timeslistened + 1 WHERE songid IN (SELECT songid FROM songalbum WHERE albumid = {album.albumid})", database);
        }
    }
}
