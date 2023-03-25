using Npgsql;

public class Listens {
    public static int? UserCountForSong(NpgsqlConnection database, int songid) {
        if (Users.LoggedInUser == null) return null;
        var cmd = new NpgsqlCommand(
                $"SELECT count FROM listen WHERE userid={Users.LoggedInUser.userid} AND songid={songid}",
                database
        );
        var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            int count = (int)reader["count"];
            reader.Close();
            return count;
        }
        reader.Close();
        return null;
    }
}
