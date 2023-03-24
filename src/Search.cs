using Npgsql;

record SearchSong(string title, decimal length, int timeslistened, string name);
record SearchArtist(int artistid);

class Search
{
    private static SearchSong searchReaderToSong(NpgsqlDataReader reader)
    {
        return new SearchSong((string)reader["title"], (decimal)reader["length"], (int)reader["timeslistened"], (string)reader["name"]);
    }

    private static SearchArtist artistReaderToArtist(NpgsqlDataReader reader)
    {
        return new SearchArtist((int)reader["artistid"]);
    }

    private const string BASE_QUERY = "SELECT * FROM song";

    public static void HandleInput(NpgsqlConnection database)
    {
        string? title = null;
        string? artist = null;
        string? album = null;
        string? genre = null;
        int limit = 10;
        while (true)
        {
            Console.WriteLine("Current song list:");
            printQuery(database, title, artist, album, genre, limit);
            Console.WriteLine("Search commands: back, filter title|artist|album|genre, limit");
            string? input = Console.ReadLine();
            switch (input)
            {
                case "filter title":
                    title = SelectTitle(database);
                    break;
                case "filter artist":
                    artist = SelectArtist(database);
                    break;
                case "filter album":
                    album = SelectAlbum(database);
                    break;
                case "filter genre":
                    genre = SelectGenre(database);
                    break;
                case "limit":
                    if (!int.TryParse(Console.ReadLine(), out limit))
                    {
                        Console.WriteLine("Couldn't parse input.");
                    }
                    break;
                case "back":
                    return;
                default:
                    Console.WriteLine("Unknown command, try again or back");
                    break;
            }
        }
    }

    private static string queryByTitle(string table, string title)
    {
        return $"SELECT * FROM ({table}) as s WHERE UPPER(s.title) LIKE UPPER('%{title}%')";
    }

    private static string queryByArtist(string table, string name)
    {
        return $"SELECT s.* FROM ({table}) as s join artistsong ars on s.songid=ars.songid join artist a on ars.artistid=a.artistid WHERE UPPER(a.name) LIKE UPPER('%{name}%')";
    }

    private static string queryByAlbum(string table, string name)
    {
        return $"SELECT s.* FROM ({table}) as s join songalbum sa on s.songid=sa.songid join album a on sa.albumid=a.albumid WHERE UPPER(a.name) LIKE UPPER('%{name}%')";
    }

    private static string queryByGenre(string table, string name)
    {
        return $"SELECT s.* FROM ({table}) as s join songgenre sg on s.songid=sg.songid join genre g on sg.genreid=g.genreid WHERE UPPER(g.name) LIKE UPPER('%{name}%')";
    }

    private static void printQuery(
            NpgsqlConnection database,
            string? title,
            string? artist,
            string? album,
            string? genre,
            int limit
    )
    {
        string query = BASE_QUERY;
        if (title != null) query = queryByTitle(query, title);
        if (artist != null) query = queryByArtist(query, artist);
        if (album != null) query = queryByAlbum(query, album);
        if (genre != null) query = queryByGenre(query, genre);
        query += $" limit {limit}";
        var cmd = new NpgsqlCommand(query, database);
        var reader = cmd.ExecuteReader();
        List<Song> songs = new List<Song>();
        while (reader.Read())
        {
            songs.Add(Songs.readerToSong(reader));
        }
        reader.Close();
        for (int i = 0; i < songs.Count; i++)
        {
            Console.WriteLine("    " + (i+1) + ". " + Songs.FormatSong(database, songs[i]));
        }
    }

    public static string? SelectTitle(NpgsqlConnection database)
    {
        Console.WriteLine("Enter part of song title or back:");

        while (true)
        {
            var title = Console.ReadLine();
            if (title == "back") return null;
            var cmd = new NpgsqlCommand($"SELECT title FROM song WHERE UPPER(title) LIKE UPPER('%{title}%')", database);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                reader.Close();
                return title;
            }
            reader.Close();
            Console.WriteLine("Couldn't find any, try again");
        }
    }

    public static string? SelectArtist(NpgsqlConnection database)
    {
        Console.WriteLine("Enter part of artist name or back:");

        while (true)
        {
            var name = Console.ReadLine();
            if (name == "back") return null;
            var cmd = new NpgsqlCommand($"SELECT artistid FROM artist WHERE UPPER(name) LIKE UPPER('%{name}%')", database);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                reader.Close();
                return name;
            }
            reader.Close();

            Console.WriteLine("Couldn't find any, try again");
        }
    }

    public static string? SelectAlbum(NpgsqlConnection database)
    {
        Console.WriteLine("Enter part of album name or back:");

        while (true)
        {
            var name = Console.ReadLine();
            if (name == "back") return null;
            var cmd = new NpgsqlCommand($"SELECT albumid FROM album WHERE UPPER(name) LIKE UPPER('%{name}%')", database);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                reader.Close();
                return name;
            }
            reader.Close();

            Console.WriteLine("Couldn't find any, try again");
        }
    }

    public static string? SelectGenre(NpgsqlConnection database)
    {
        Console.WriteLine("Enter part of genre name or back:");

        while (true)
        {
            var name = Console.ReadLine();
            if (name == "back") return null;
            var cmd = new NpgsqlCommand($"SELECT genreid FROM genre WHERE UPPER(name) LIKE UPPER('%{name}%')", database);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                reader.Close();
                return name;
            }
            reader.Close();

            Console.WriteLine("Couldn't find any, try again");
        }
    }
}
