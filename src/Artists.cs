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
        var query = new NpgsqlCommand($"SELECT * FROM artist WHERE artistid IN (SELECT artistid FROM artistsong WHERE songid={songid})", database);
        var reader = query.ExecuteReader();

        List<Artist> artists = new List<Artist>();
        while (reader.Read())
        {
            artists.Add(ReaderToArtist(reader));
        }

        reader.Close();

        return artists;
    }
}
