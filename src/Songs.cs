using Npgsql;

record Song(int songid, string title, decimal length, DateTime releasedate, int timeslistened);
record SearchSong(string title, decimal length, int timeslistened, string name, string albumName);
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

    private static SearchSong searchReaderToSong(NpgsqlDataReader reader, string albumName)
    {
        return new SearchSong((string)reader["title"], (decimal)reader["length"], (int)reader["timeslistened"], (string)reader["name"], albumName);
    }

    private static SearchArtist artistReaderToArtist(NpgsqlDataReader reader)
    {
        return new SearchArtist((int)reader["artistid"]);
    }

    private static SongIDs songIDReaderToSongID(NpgsqlDataReader reader)
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

    public static List<SearchSong>? SearchSongByTitle(NpgsqlConnection database, String title)
    {
        // Make sure song name exists
        if (title.Length > 0)
        {
            // Get song like name
            var cmd = new NpgsqlCommand($"SELECT title, length, songid, timeslistened, a2.name FROM song LEFT JOIN artistsong a on song.songid = a.songid LEFT JOIN artist a2 on a.artistid = a2.artistid WHERE UPPER(song.title) LIKE UPPER('{title}') AND a2.name IS NOT NULL;", database);
            var reader = cmd.ExecuteReader();
            var returnSongs = new List<SearchSong>();

            while (reader.Read())
            {
                int songId = (int)reader["songid"];
                var album = Albums.GetAlbumForSong(database, songId);
                if (album == null) continue;
                returnSongs.Add(searchReaderToSong(reader, album.name));
            }
            reader.Close();
            return returnSongs;
        }
        else
        {
            return null;
        }
    }

    public static List<SearchSong>? SearchSongByArtist(NpgsqlConnection database, String artistName)
    {

        if (artistName.Length > 0)
        {
            // Get artist ids first
            var cmd = new NpgsqlCommand($"SELECT artistid FROM artist WHERE name LIKE '{artistName}'", database);
            var reader = cmd.ExecuteReader();
            var artists = new List<SearchArtist>();

            while (reader.Read())
            {
                artists.Add(artistReaderToArtist(reader));
            }
            reader.Close();

            // Get song IDs matching artist IDs
            var songIDs = new List<SongIDs>();
            foreach (var id in artists)
            {
                var cmd1 = new NpgsqlCommand($"SELECT songid FROM artistsong WHERE artistid = {id.artistid}", database);
                var reader1 = cmd1.ExecuteReader();
                while (reader1.Read())
                {
                    songIDs.Add(songIDReaderToSongID(reader1));
                }
                reader1.Close();
            }

            // Get the rest of the song data matching the song IDs
            var returnSongs = new List<SearchSong>();
            foreach (var id in songIDs)
            {
                var cmd3 = new NpgsqlCommand($"SELECT s.songid, s.title, s.length, s.timeslistened, a2.name FROM song s LEFT JOIN artistsong a on s.songid = a.songid LEFT JOIN artist a2 on a.artistid = a2.artistid WHERE s.songid = {id.songid} AND a2.name IS NOT NULL;", database);
                var reader3 = cmd3.ExecuteReader();

                while (reader3.Read())
                {
                    int songId = (int)reader["songid"];
                    var album = Albums.GetAlbumForSong(database, songId);
                    if (album == null) continue;
                    returnSongs.Add(searchReaderToSong(reader3, album.name));
                }
                reader3.Close();
            }
            return returnSongs;
        }
        else
        {
            return null;
        }
    }

    public static List<SearchSong>? SearchSongByAlbum(NpgsqlConnection database, String albumName)
    {
        var albumQuery = new NpgsqlCommand($"SELECT * FROM album WHERE name LIKE '{albumName}'", database);
        var reader = albumQuery.ExecuteReader();
        if (reader.Read())
        {
            var album = Albums.readerToAlbum(reader);
            reader.Close();

            var songIdQuery = new NpgsqlCommand($"SELECT songId FROM songalbum WHERE albumid = {album.albumid}", database);
            var songIdReader = songIdQuery.ExecuteReader();

            List<int> songIds = new List<int>();
            while (songIdReader.Read())
            {
                songIds.Add(songIdReader.GetInt32(0));
            }
            songIdReader.Close();

            Dictionary<int, List<Artist>> songIdArtists = new Dictionary<int, List<Artist>>();
            List<Song> songs = new List<Song>();
            foreach (int songId in songIds)
            {
                songIdArtists.Add(songId, Artists.ForSong(database, songId));
                var songInfoQuery = new NpgsqlCommand($"SELECT * FROM song WHERE songid = {songId}", database);
                var songInfoReader = songInfoQuery.ExecuteReader();
                if (songInfoReader.Read())
                {
                    songs.Add(readerToSong(songInfoReader));
                }

                songInfoReader.Close();
            }

            List<SearchSong> searchSongs = new List<SearchSong>();
            foreach (Song song in songs)
            {
                var artists = songIdArtists[song.songid];
                if (artists.Count == 0) continue;
                SearchSong searchSong = new SearchSong(song.title, song.length, song.timeslistened, artists[0].name, album.name);
                searchSongs.Add(searchSong);
            }

            return searchSongs;
        }

        return null;
    }

    // this query has fairly similar logic as searchsongbyalbum
    public static List<SearchSong>? SearchSongByGenre(NpgsqlConnection database, String genreName)
    {
        var genreQuery = new NpgsqlCommand($"SELECT * FROM genre WHERE name = '{genreName}'", database);
        var reader = genreQuery.ExecuteReader();
        if (reader.Read())
        {
            var genre = Genres.readerToGenre(reader);
            reader.Close();

            var songIdQuery = new NpgsqlCommand($"SELECT songId FROM songgenre WHERE genreid = {genre.genreid}", database);
            var songIdReader = songIdQuery.ExecuteReader();

            List<int> songIds = new List<int>();
            while (songIdReader.Read())
            {
                songIds.Add(songIdReader.GetInt32(0));
            }
            songIdReader.Close();


            Dictionary<int, List<Artist>> songIdArtists = new Dictionary<int, List<Artist>>();
            Dictionary<int, Album?> songIdAlbum = new Dictionary<int, Album?>();
            List<Song> songs = new List<Song>();
            foreach (int songId in songIds)
            {
                songIdArtists.Add(songId, Artists.ForSong(database, songId));
                songIdAlbum.Add(songId, Albums.GetAlbumForSong(database, songId));
                var songInfoQuery = new NpgsqlCommand($"SELECT * FROM song WHERE songid = {songId}", database);
                var songInfoReader = songInfoQuery.ExecuteReader();
                if (songInfoReader.Read())
                {
                    songs.Add(readerToSong(songInfoReader));
                }

                songInfoReader.Close();
            }

            List<SearchSong> searchSongs = new List<SearchSong>();
            foreach (Song song in songs)
            {
                var artists = songIdArtists[song.songid];
                var songAlbum = songIdAlbum[song.songid];
                if (artists.Count == 0) continue;
                SearchSong searchSong = new SearchSong(song.title, song.length, song.timeslistened, artists[0].name, songAlbum.name);
                searchSongs.Add(searchSong);
            }

            return searchSongs;
        }
        return null;
    }

    public static string FormatSong(NpgsqlConnection database, Song song)
    {
        var artists = string.Join(", ", Artists.ForSong(database, song.songid).Select(a => a.name));
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
