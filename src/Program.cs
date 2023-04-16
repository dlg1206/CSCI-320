using Npgsql;
using Renci.SshNet;
using DotNetEnv;

public class DBTest
{
    private static readonly string _logoFile = "src/logo";
    public static void Main(string[] args)
    {

        try
        {
            if (args[0].Equals("-dev"))
            {
                Console.WriteLine("Command File detected");
                var cmdFiles = new List<string>(args);
                cmdFiles.RemoveAt(0);   // remove '-dev' flag
                Util.InitPresetCommands(cmdFiles);
            }
        }
        catch (Exception e)
        {
            // ignored
        }


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
                    
                    // attempt to print logo if logo file exists
                    if(File.Exists(_logoFile))
                        Console.WriteLine(File.ReadAllText(_logoFile));
                        
                    Input.HandleInput(conn);
                }
            }
            finally
            {
                Console.WriteLine("Bye.");
                conn?.Close();
            }
        }
    }
}
