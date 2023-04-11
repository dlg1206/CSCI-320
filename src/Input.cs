using Npgsql;

class Input
{
    /// <summary>
    /// Prints command line usages
    /// </summary>
    private static void PrintCommands()
    {
        Console.WriteLine("Login to Account:    login <username> <password>");
        Console.WriteLine("Search songs:        search");
        Console.WriteLine("Exit the System:     exit");
    }
    
    // takes the database handle, which is just passed from function to function
    public static void HandleInput(NpgsqlConnection database)
    {
        
        PrintCommands();
        Console.Write("$ ");
        var input = Console.ReadLine();
        while (input != null)
        {
            // Switch on keyword
            switch (input.Split(" ")[0].ToLower())
            {
                // Attempt login to account
                case "login":
                    Users.HandleInput(database);
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
                    Console.WriteLine("[SERVER] | \"" + input.Split(" ")[0] + "\" is not a valid input");
                    break;
            }
            // get next input
            Console.Write("$");
            input = Console.ReadLine();
        }

    }
}
