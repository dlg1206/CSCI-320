using Npgsql;

public class Listens {
    public static int? UserCountForSong(NpgsqlConnection database, int songid) {
        if (Users.LoggedInUser == null) return null;
        var cmd = new NpgsqlCommand(
                $"SELECT count(timestamp) FROM listen WHERE userid={Users.LoggedInUser.userid} AND songid={songid}",
                database
        );
        var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            long count = (long)reader["count"];
            reader.Close();
            return (int)count;
        }
        reader.Close();
        return null;
    }
}
