using Npgsql;

record Artist(
        int artistid,
        string name
);

class Artists
{
    private static Artist ReaderToArtist(NpgsqlDataReader reader)
    {
        return new Artist(
                (int)reader["artistid"],
                (string)reader["name"]
        );
    }

    public static List<Artist> ForSong(NpgsqlConnection database, int songid)
    {
        var query = new NpgsqlCommand(
                $"SELECT a.* FROM artistsong s join artist a on s.artistid=a.artistid WHERE s.songid={songid}",
                database
        );
        var reader = query.ExecuteReader();

        List<Artist> artists = new List<Artist>();
        while (reader.Read())
        {
            artists.Add(ReaderToArtist(reader));
        }

        reader.Close();

        return artists;
    }

    /// <summary>
    /// Gets an artist based on id
    /// </summary>
    /// <param name="database">db to query</param>
    /// <param name="artistId">artist id</param>
    /// <returns></returns>
    public static Artist GetArtistById(NpgsqlConnection database, int artistId)
    {
        // Prepare query
        var cmd = new NpgsqlCommand($"SELECT * FROM artist WHERE artistId={artistId}", database);
        var reader = cmd.ExecuteReader();
        reader.Read();
        var artist = new Artist((int) reader["artistid"], (string) reader["name"]);
        reader.Close();
        return artist;
    }
}
