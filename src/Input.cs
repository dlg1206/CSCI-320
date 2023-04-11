using Npgsql;

class Input
{
    private static string _serverPrefix = "guest@spotify2$ ";
    /// <summary>
    /// Prints command line usages
    /// </summary>
    private static void PrintCommands()
    {
        Console.WriteLine("Login to Account:    login <username> <password>");
        Console.WriteLine("Search songs:        search");
        Console.WriteLine("Exit the System:     exit");
    }
    
    /// <summary>
    /// Handles inputs from guest, or user that isn't logged in
    /// </summary>
    /// <param name="database">Database to access</param>
    public static void HandleInputGuest(NpgsqlConnection database)
    {
        
        PrintCommands();
        Console.Write(_serverPrefix);
        var input = Console.ReadLine();
        while (input != null)
        {
            var inputArgs = input.Split(" ");
            // Switch on keyword
            switch (inputArgs[0].ToLower())
            {
                // Attempt login to account
                case "login":
                    // login with username and password if given, else use login prompt
                    if (inputArgs.Length == 3) 
                        Users.LogIn(database, inputArgs[1], inputArgs[2]);
                    else 
                        Users.LogIn(database, null, null);
                    break;
                // Search
                case "search":
                    Search.HandleInput(database);
                    break;
                case "help":
                    PrintCommands();
                    break;
                // Exit
                case "exit":
                    return;
                // Unknown command
                default:
                    Console.WriteLine("[SERVER] | \"" + input.Split(" ")[0] + "\" is not a valid input");
                    break;
            }
            // get next input
            Console.Write(_serverPrefix);
            input = Console.ReadLine();
        }
    }
    
    /// <summary>
    /// Handles prompts from User who is logged in
    /// </summary>
    /// <param name="database">database to access</param>
    public static void HandleInputUser(NpgsqlConnection database)
    {

        PrintCommands();
        Console.Write("$ ");
        var input = Console.ReadLine();
        while (input != null)
        {
            var inputArgs = input.Split(" ");
            // Switch on keyword
            switch (inputArgs[0].ToLower())
            {
                // Attempt login to account
                case "login":
                    // login with username and password if given, else use login prompt
                    if (inputArgs.Length == 3) 
                        Users.LogIn(database, inputArgs[1], inputArgs[2]);
                    else 
                        Users.LogIn(database, null, null);
                    break;
                // Search
                case "search":
                    Search.HandleInput(database);
                    break;
                // Exit
                case "exit":
                    return;
                // Unknown command
                default:
                    Console.WriteLine("[SERVER] | \"" + inputArgs[0] + "\" is not a valid input");
                    break;
            }

            // get next input
            Console.Write("$");
            input = Console.ReadLine();
        }
    }
}
