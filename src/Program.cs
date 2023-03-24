using Npgsql;
using Renci.SshNet;
using DotNetEnv;

public class DBTest
{
    public static void Main(string[] args)
    {
        Env.Load();
        string username = DotNetEnv.Env.GetString("DB_USER");
        string password = DotNetEnv.Env.GetString("DB_PASS");
        const string dbName = "p320_18";

        NpgsqlConnection? conn = null;
        var connInfo = new PasswordConnectionInfo("starbug.cs.rit.edu", username, password);

        using (var sshClient = new SshClient(connInfo))
        {
            sshClient.Connect();
            Console.WriteLine("Connection Established");
            var port = new ForwardedPortLocal("127.0.0.1", "127.0.0.1", 5432);
            sshClient.AddForwardedPort(port);
            port.Start();
            Console.WriteLine("Port Forwarded");
            var connString =
                $"Server={port.BoundHost};Database={dbName};Port={port.BoundPort};User Id={username};Password={password};";

            try
            {
                using (conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    Console.WriteLine("Connected to DB");
                    Input.HandleInput(conn);
                    // Users.GetUsers(conn);
                    // Console.WriteLine(Users.LogIn(conn, "tommy", "pass"));
                    // Console.WriteLine(Users.LogIn(conn, "test", "pass"));
                    // Console.WriteLine(Users.CreateUser(conn, "test@email.com", "testUser", "firstname", "lastname", new DateOnly(2022, 04, 20), "password1"));

                        
                    // foreach(var song in Songs.SearchSongByTitle(conn, "TEST TEST TEST"))
                    // {
                    //     Console.WriteLine(song);
                    // }
                    // foreach(var song in Songs.SearchSongByArtist(conn, "Shayna McKevin"))
                    // {
                    //     Console.WriteLine(song);
                    // }
                        

                }
            }
            finally
            {
                conn?.Close();
            }
        }
    }
}
