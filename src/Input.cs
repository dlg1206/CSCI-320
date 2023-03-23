using Npgsql;

class Input
{
    // takes the database handle, which is just passed from function to function
    public static void HandleInput(NpgsqlConnection database)
    {
        Console.WriteLine("Possible inputs are: user, exit");
        string? input = Console.ReadLine();
        if (input != null)
        {
            switch (input.ToLower())
            {
                case "user":
                    Users.HandleInput(database);
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