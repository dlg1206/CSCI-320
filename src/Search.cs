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

    public static void HandleInput(NpgsqlConnection database)
    {
        string? title = null;
        string? artist = null;
        string? album = null;
        string? genre = null;

        int titleSort = 0;
        int artistSort = 0;
        int genreSort = 0;
        int releaseSort = 0;

        int limit = 10;
        while (true)
        {
            printQuery(database, title, artist, album, genre, titleSort, artistSort, genreSort, releaseSort, limit);
            Console.WriteLine("Search commands: back, filter title|artist|album|genre, sort title|artist|genre|release, limit");
            var input = Util.GetServerPrompt("/songs");
            switch (input)
            {
                case "filter title":
                    title = SelectTitle(database, title);
                    break;
                case "filter artist":
                    artist = SelectArtist(database, artist);
                    break;
                case "filter album":
                    album = SelectAlbum(database, album);
                    break;
                case "filter genre":
                    genre = SelectGenre(database, genre);
                    break;
                case "sort title":
                    titleSort = (titleSort + 1) % 3;
                    break;
                case "sort artist":
                    artistSort = (artistSort + 1) % 3;
                    break;
                case "sort genre":
                    genreSort = (genreSort + 1) % 3;
                    break;
                case "sort release":
                    releaseSort = (releaseSort + 1) % 3;
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
            Console.WriteLine();
        }
    }

    private static void printQuery(
            NpgsqlConnection database,
            string? title,
            string? artist,
            string? album,
            string? genre,
            int ts,
            int ars,
            int gs,
            int rs,
            int limit
    )
    {
        string joins = "";
        List<string> wheres = new List<string>();
        List<string> whereout = new List<string>();
        List<string> orders = new List<string>();
        List<string> orderout = new List<string>();

        if (artist != null || ars > 0) joins += " join artistsong ars on s.songid=ars.songid join artist ar on ars.artistid=ar.artistid";
        if (album != null) joins += $" join songalbum sa on s.songid=sa.songid join album al on sa.albumid=al.albumid";
        if (genre != null || gs > 0) joins += $" join songgenre sg on s.songid=sg.songid join genre g on sg.genreid=g.genreid";

        if (title != null) {
            wheres.Add($"UPPER(s.title) LIKE UPPER('%{title}%')");
            whereout.Add($"title '{title}'");
        }
        if (artist != null) {
            wheres.Add($"UPPER(ar.name) LIKE UPPER('%{artist}%')");
            whereout.Add($"artist '{artist}'");
        }
        if (album != null) {
            wheres.Add($"UPPER(al.name) LIKE UPPER('%{album}%')");
            whereout.Add($"album {album}");
        }
        if (genre != null) {
            wheres.Add($"UPPER(g.name) LIKE UPPER('%{genre}%')");
            whereout.Add($"genre {genre}");
        }

        if (ars > 0)
        {
            orders.Add(" ar.name " + Sort(ars));
            orderout.Add("artist " + Sort(ars));
        }
        if (rs > 0)
        {
            orders.Add(" s.releasedate " + Sort(rs));
            orderout.Add("release date " + Sort(rs));
        }
        if (gs > 0)
        {
            orders.Add(" g.name " + Sort(gs));
            orderout.Add("genre " + Sort(gs));
        }
        if (ts > 0)
        {
            orders.Add(" s.title " + Sort(ts));
            orderout.Add("title " + Sort(ts));
        }

        string query = $"SELECT s.* from song s{joins}";
        if (wheres.Count > 0) query += $" WHERE {string.Join(" AND ", wheres)}";
        if (orders.Count > 0) query += $" ORDER BY {string.Join(", ", orders)}";


        query = $"SELECT s.* FROM ({query}) as s limit {limit}";

        var cmd = new NpgsqlCommand(query, database);
        var reader = cmd.ExecuteReader();
        List<Song> songs = new List<Song>();
        while (reader.Read())
        {
            songs.Add(Songs.readerToSong(reader));
        }
        reader.Close();

        Console.WriteLine("Current song list:");

        for (int i = 0; i < songs.Count; i++)
        {
            Console.WriteLine("    " + (i + 1) + ". " + Songs.FormatSong(database, songs[i]));
        }

        if (whereout.Count > 0) {
            Console.WriteLine("Filters: "+string.Join(", ", whereout));
        }
        if (orderout.Count > 0) {
            Console.WriteLine("Sorts: "+string.Join(", ", orderout));
        }
    }

    public static string? SelectTitle(NpgsqlConnection database, string? old)
    {

        while (true)
        {
            var title = Util.GetInput("Enter part of song title or back: ");
            if (title == "back") return old;
            if (title == "") return null;
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

    public static string? SelectArtist(NpgsqlConnection database, string? old)
    {

        while (true)
        {
            var name = Util.GetInput("Enter part of artist name or back: ");
            if (name == "back") return old;
            if (name == "") return null;
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

    public static string? SelectAlbum(NpgsqlConnection database, string? old)
    {

        while (true)
        {
            var name = Util.GetInput("Enter part of album name or back: ");
            if (name == "back") return old;
            if (name == "") return null;
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

    public static string? SelectGenre(NpgsqlConnection database, string? old)
    {

        while (true)
        {
            var name = Util.GetInput("Enter part of genre name or back: ");
            if (name == "back") return old;
            if (name == "") return null;
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

    private static string Sort(int n)
    {
        return n == 2 ? "desc" : "asc";
    }
}
