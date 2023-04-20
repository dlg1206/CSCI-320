using Npgsql;

class Rankings
{
    public static void ListTopFiveGenres(NpgsqlConnection database)
    {
        var cmd = new NpgsqlCommand(
                $"SELECT g.* FROM genre as g join songgenre as sg on g.genreid = sg.genreid join listen as l on sg.songid = l.songid GROUP BY g.genreid ORDER BY count(l.timestamp) desc limit 5",
                database
        );
        var reader = cmd.ExecuteReader();
        var genres = new List<Genre>();
        while (reader.Read())
        {
            genres.Add(Genres.readerToGenre(reader));
        }
        reader.Close();

        Console.WriteLine("-= Top 5 Genres =-");
        var genreCount = 1;
        // get each artist from db based on id
        foreach (var genre in genres)
        {
            Console.WriteLine($"{genreCount++}: {genre.name}");
        }
    }

    public static void ForYou(NpgsqlConnection database)
    {
        int uid = Users.LoggedInUser!.userid;
        var cmd = new NpgsqlCommand(
                $@"
SELECT songid, coalesce(gscore, 0) + coalesce(ascore, 0) + coalesce(suscore, 0) as score FROM (
    SELECT songid, sum(times) as gscore from (
        SELECT songid from song WHERE not exists(SELECT * FROM listen WHERE userid = {uid} and songid = song.songid)
    ) as s natural join songgenre natural join (
        SELECT genreid, times FROM (
            SELECT s.songid, count(*) as times FROM listen as l natural join song s WHERE l.userid = {uid} GROUP BY s.songid
        ) as lc natural join songgenre ORDER BY times desc
    ) as sc GROUP BY songid ORDER BY gscore desc
) as gi natural full join (
    SELECT songid, sum(times) as ascore from (
        SELECT songid from song WHERE not exists(SELECT * FROM listen WHERE userid = {uid} and songid = song.songid)
    ) as s natural join artistsong natural join (
        SELECT artistid, times FROM (
            SELECT s.songid, count(*) as times FROM listen as l natural join song s WHERE l.userid = {uid} GROUP BY s.songid
        ) as lc natural join artistsong ORDER BY times desc
    ) as sc GROUP BY songid ORDER BY ascore desc
) as ai natural full join (
    SELECT songid, round(sum(uscore)/30) as suscore FROM (
        SELECT songid, userid FROM listen WHERE not exists(SELECT * from listen as l where l.songid = listen.songid and userid = {uid})
    ) as su natural join (
        SELECT userid, count(songid) as uscore FROM (
            SELECT s.songid, count(*) as times FROM listen as l natural join song s WHERE l.userid = {uid} GROUP BY s.songid
        ) as lc natural join listen WHERE userid != {uid} GROUP BY userid, songid ORDER BY count(timestamp) desc
    ) as uc GROUP BY songid ORDER BY suscore desc
) as ui ORDER BY score desc LIMIT 20
                ", database
        );
        var reader = cmd.ExecuteReader();
        var songids = new List<int>();
        while (reader.Read())
        {
            songids.Add((int) reader["songid"]);
        }
        reader.Close();

        Console.WriteLine("-= For You =-");
        Songs.PrintSongs(database, Songs.GetSongs(database, songids));
    }
}
