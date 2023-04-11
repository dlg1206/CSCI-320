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
        string? input = Console.ReadLine();
        if (input != null)
        {
            switch (input.ToLower())
            {
                case "user":
                    Users.HandleInput(database);
                    HandleInput(database);
                    break;
                case "search":
                    Search.HandleInput(database);
                    HandleInput(database);
                    break;
                case "exit":
                    break;
                default:
                    Console.WriteLine("Not a valid input");
                    HandleInput(database);
                    break;
            }
        }
        else
        {
            Console.WriteLine("null input, please retry");
            HandleInput(database);
        }

    }
}
