using Npgsql;

record Playlist(int playlistid, int userid, DateTime creationdate, string playlistname);

class Playlists
{
    private static Playlist readerToPlaylist(NpgsqlDataReader reader)
    {
        return new Playlist((int)reader["playlistid"], (int)reader["userid"], (DateTime)reader["creationdate"], (string)reader["playlistname"]);
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
        var query = new NpgsqlCommand($"SELECT * FROM playlist WHERE userid={userid}", database);
        var reader = query.ExecuteReader();

        List<Playlist> playlists = new List<Playlist>();
        while (reader.Read())
        {
            playlists.Add(readerToPlaylist(reader));
        }

        return playlists;
    }

    public static void InsertSongIntoPlaylist(NpgsqlConnection database, int playlistid, int songid)
    {
        var insert = new NpgsqlCommand($"INSERT INTO songplaylist(songid, playlistid) VALUES ({songid}, {playlistid})", database);
        insert.Prepare();
        insert.ExecuteNonQuery();
    }
}