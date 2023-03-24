using Npgsql;

record Playlist(int playlistid, int userid, DateTime creationdate, string playlistname);

class Playlists
{
    private static Playlist readerToPlaylist(NpgsqlDataReader reader)
    {
        return new Playlist((int)reader["playlistid"], (int)reader["userid"], (DateTime)reader["creationdate"], (string)reader["playlistname"]);
    }

    public static void HandleInput(NpgsqlConnection database)
    {
        Console.WriteLine("Playlist input possibilities: create, list, rename, delete");
        string? input = Console.ReadLine();
        if (input != null)
        {
            switch (input)
            {
                case "create":
                    Playlists.MakePlaylist(database);
                    break;
                case "list":
                    // this is guaranteed to not be null
                    Playlists.DisplayPlaylists(database, Users.LoggedInUser!.userid);
                    break;
                case "rename":
                    Playlists.RenamePlaylist(database);
                    break;
                case "delete":
                    Playlists.DeletePlaylist(database);
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

    private static void DeletePlaylist(NpgsqlConnection database)
    {
        Console.WriteLine("Enter the playlist to delete");
        var playlistName = Console.ReadLine();

        List<Playlist> playlists = GetPlaylistsForUser(database, Users.LoggedInUser!.userid);
        var playlistId = playlists.Find(x => x.playlistname == playlistName)?.playlistid;

        if (playlistId != null)
        {
            var deleteSongPlaylistReference = new NpgsqlCommand($"DELETE FROM songplaylist WHERE playlistid = {playlistId}", database);
            deleteSongPlaylistReference.Prepare();
            deleteSongPlaylistReference.ExecuteNonQuery();
        }

        var delete = new NpgsqlCommand($"DELETE FROM playlist WHERE playlistname = '{playlistName}' AND userid = {Users.LoggedInUser!.userid}", database);
        delete.Prepare();
        delete.ExecuteNonQuery();
    }

    private static void RenamePlaylist(NpgsqlConnection database)
    {
        Console.WriteLine("Enter the playlist to rename");
        var playlistName = Console.ReadLine();
        Console.WriteLine("Enter the new playlist name");
        var newPlaylistName = Console.ReadLine();

        var update = new NpgsqlCommand($"UPDATE playlist SET playlistname = '{newPlaylistName}' WHERE playlistname = '{playlistName}' AND userid = {Users.LoggedInUser!.userid}", database);
        update.Prepare();
        update.ExecuteNonQuery();
    }

    public static void DisplayPlaylists(NpgsqlConnection database, int userid)
    {
        List<Playlist> playlists = GetPlaylistsForUser(database, userid);
        Console.WriteLine($"\nPlaylists: ");
        foreach (var playlist in playlists)
        {
            (float duration, int numSongs) = GetPlaylistDurationAndNumSongs(database, playlist.playlistid);
            Console.WriteLine($"Playlist: {playlist.playlistname}, Duration: {duration} seconds, Number of Songs: {numSongs}");
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