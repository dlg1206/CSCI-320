using Npgsql;

record Genre(int genreid, string name);

class Genres
{
    public static Genre readerToGenre(NpgsqlDataReader reader)
    {
        return new Genre((int)reader["genreid"], (string)reader["name"]);
    }
}