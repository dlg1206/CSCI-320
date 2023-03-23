using Npgsql;

record Playlist(int playlistid, int userid, DateTime creationdate, string playlistname);

class Playlists
{
    private static Playlist readerToPlaylist(NpgsqlDataReader reader)
    {
        return new Playlist((int)reader["playlistid"], (int)reader["userid"], (DateTime)reader["creationdate"], (string)reader["playlistname"]);
    }

    public static void DisplayPlaylists(NpgsqlConnection database, int userid)
    {
        List<Playlist> playlists = GetPlaylistsForUser(database, userid);
        Console.WriteLine($"\nPlaylists: ");
        foreach (var playlist in playlists)
        {
            (float duration, int numSongs) = GetPlaylistDurationAndNumSongs(database, playlist.playlistid);
            Console.WriteLine($"Playlist: {playlist.playlistname}, Duration: {duration} minutes, Number of Songs: {numSongs}");
        }
        Console.WriteLine("------------");
    }

    public static void MakePlaylist(NpgsqlConnection database)
    {
        Console.WriteLine("Enter playlist name");
        var playlistName = Console.ReadLine();

        var insert = new NpgsqlCommand("INSERT INTO playlist(userid, creationdate, playlistname) VALUES ($1, $2, $3)", database)
        {
            Parameters = {
                new() { Value = Users.LoggedInUser?.userid },
                new() { Value = DateTime.Now },
                new() { Value = playlistName }
            }
        };

        insert.Prepare();
        insert.ExecuteNonQuery();
    }

    public static List<Playlist> GetPlaylistsForUser(NpgsqlConnection database, int userid)
    {
        var query = new NpgsqlCommand($"SELECT * FROM playlist WHERE userid={userid} ORDER BY playlistname ASC", database);
        var reader = query.ExecuteReader();

        List<Playlist> playlists = new List<Playlist>();
        while (reader.Read())
        {
            playlists.Add(readerToPlaylist(reader));
        }

        reader.Close();

        return playlists;
    }

    public static (float, int) GetPlaylistDurationAndNumSongs(NpgsqlConnection database, int playlistId)
    {
        var query = new NpgsqlCommand($"SELECT SUM(length), COUNT(*) FROM (SELECT * FROM song, songplaylist WHERE playlistid = {playlistId} AND song.songid = songplaylist.songid) as songs", database);
        var reader = query.ExecuteReader();
        while (reader.Read())
        {
            float sum;
            try
            {
                sum = reader.GetFloat(0);
            }
            catch (InvalidCastException)
            {
                sum = 0;
            }
            var numSongs = reader.GetInt32(1);
            reader.Close();
            return (sum, numSongs);
        }

        reader.Close();
        return (-1, 0);
    }

    public static void InsertSongIntoPlaylist(NpgsqlConnection database, int playlistid, int songid)
    {
        var insert = new NpgsqlCommand($"INSERT INTO songplaylist(songid, playlistid) VALUES ({songid}, {playlistid})", database);
        insert.Prepare();
        insert.ExecuteNonQuery();
    }
}