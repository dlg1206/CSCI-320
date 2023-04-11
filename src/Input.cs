using Npgsql;

class Input
{
    private static string _userName = "guest";
    private const string ServerName = "@spotify2$ ";
    /// <summary>
    /// Prints command line usages for guest
    /// </summary>
    private static void PrintGuestCommands()
    {
        Console.WriteLine("=========Options==========");
        Console.WriteLine("Login to Account:    login");
        Console.WriteLine("Create new Account:  new");
        Console.WriteLine("Search all songs:    search");
        Console.WriteLine("Exit the System:     exit");
    }
    
    /// <summary>
    /// Prints command line usages for user
    /// </summary>
    private static void PrintUserCommands()
    {
        Console.WriteLine("=================Friends=================");
        Console.WriteLine("List your friends:       friends");
        Console.WriteLine("Follow a User:           follow <email>");
        Console.WriteLine("Unfollow a User:         unfollow <email>");
        Console.WriteLine("==================Songs==================");
        Console.WriteLine("Search all songs:        search");
        Console.WriteLine("Access your songs:       songs");
        Console.WriteLine("Access your playlists:   playlists");
        Console.WriteLine("=================Account=================");
        Console.WriteLine("Logout of Account:       logout");
        Console.WriteLine("Exit the System:         exit");
    }
    
    
    /// <summary>
    /// Handles inputs from guest, or user that isn't logged in
    /// </summary>
    /// <param name="database">Database to access</param>
    public static void HandleInput(NpgsqlConnection database)
    {
        PrintGuestCommands();   // print guest commands on launch
        for (;;)
        {
            var input = Util.GetInput(_userName + ServerName);
            var inputArgs = input.Split(" ");
            // Switch on keyword
            switch (inputArgs[0].ToLower())
            {
                // Attempt login to account
                case "login":
                    // can't log into someone else's account while logged in
                    if(Users.LoggedInUser != null)
                        break;
                    // on success, switch to user commands
                    if (Users.HandleInput(database, inputArgs) && Users.LoggedInUser != null)
                    {
                        _userName = Users.LoggedInUser.username;
                        PrintUserCommands();
                    }
                    break;
                
                
                // Search
                case "search":
                    Search.HandleInput(database);
                    break;
                
                // Display commands
                case "help":
                    // get correct help menu
                    if (Users.LoggedInUser == null)
                        PrintGuestCommands();
                    else 
                        PrintUserCommands();
                    break;
                
                // User commands
                
                case "friends": // list friends
                case "new":     // new user
                    if(Users.LoggedInUser != null)
                        break;
                    Users.HandleInput(database, inputArgs);
                    break;
    
                case "follow":      // follow user
                case "unfollow":    // unfollow user
                    if (Users.LoggedInUser != null)
                        Users.HandleInput(database, inputArgs);
                    break;
                
                // Access Songs
                case "songs":
                    if (Users.LoggedInUser != null)
                        Songs.HandleInput(database);
                    break;
                
                // Access playlists
                case "playlists":
                    if (Users.LoggedInUser != null)
                        Playlists.HandleInput(database);
                    break;
                
                // Exit
                case "exit":
                    return;
            }
        }
    }
    
}
