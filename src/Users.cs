using Npgsql;

record User(int userid, string email, string username, string firstName, string lastName, DateTime dob, DateTime creationDate, DateTime lastAccessed, string password);

class Users
{
    public static User? LoggedInUser { get; private set; } = null;

    public static void HandleInput(NpgsqlConnection database)
    {
        Console.WriteLine("User input possibilities: create account, songs, friends, follow, unfollow, playlists, login");
        string? input = Console.ReadLine();
        if (input != null)
        {
            switch (input.ToLower())
            {
                // case "create account":
                //     CreateUser(database);
                //     break;
                case "login":
                    LogIn(database, null, null);
                    break;
                case "songs":
                    if (LoggedInUser != null)
                    {
                        Songs.HandleInput(database);
                    }
                    else
                    {
                        Console.WriteLine("You are not logged in");
                    }
                    break;
                case "playlists":
                    if (LoggedInUser != null)
                    {
                        Playlists.HandleInput(database);
                    }
                    else
                    {
                        Console.WriteLine("You are not logged in");
                    }
                    break;
                case "friends":
                    ListFriends(database);
                    break;
                case "follow":
                    HandleFriend(database, true);
                    break;
                case "unfollow":
                    HandleFriend(database, false);
                    break;
                default:
                    Console.WriteLine("Not an input");
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

   
    

    private static User readerToUser(NpgsqlDataReader reader)
    {
        return new User((int)reader["userid"], (string)reader["email"], (string)reader["username"], (string)reader["firstname"],
                        (string)reader["lastname"], (DateTime)reader["dob"], (DateTime)reader["creationdate"],
                        (DateTime)reader["lastaccessed"], (string)reader["password"]
                        );
    }

    public static List<User> GetUsers(NpgsqlConnection database)
    {

        var cmd = new NpgsqlCommand("SELECT * FROM \"user\"", database);
        var reader = cmd.ExecuteReader();
        List<User> users = new List<User>();
        while (reader.Read())
        {
            users.Add(readerToUser(reader));
        }

        // example of how to iterate through lists
        // foreach (var user in users)
        // {
        //     Console.WriteLine(user.username + " " + user.password);
        // }

        reader.Close();
        return users;
    }

    private static void ListFriends(NpgsqlConnection database)
    {
        Console.WriteLine("Not needed in this implementation");
    }

    private static void HandleFriend(NpgsqlConnection database, bool follow)
    {
        Console.WriteLine("Enter your friends email");
        var email = Console.ReadLine();
        // technically we could consolidate this down into one query, but having the utility method isn't bad
        User? friend = GetUserFromEmail(database, email);

        if (friend == null)
        {
            Console.WriteLine("No user exists with that email");
            return;
        }

        if (follow)
        {
            AddFriend(database, friend);
        }
        else
        {
            RemoveFriend(database, friend);
        }
    }

    private static void AddFriend(NpgsqlConnection database, User friend)
    {
        if (LoggedInUser != null)
        {
            var insert = new NpgsqlCommand($"INSERT INTO friend(userid1, userid2) VALUES({LoggedInUser?.userid}, {friend.userid})", database);
            insert.Prepare();
            insert.ExecuteNonQuery();
        }
    }

    private static void RemoveFriend(NpgsqlConnection database, User friend)
    {
        if (LoggedInUser != null)
        {
            var delete = new NpgsqlCommand($"DELETE FROM friend WHERE (userid1 = {friend.userid} AND userid2 = {LoggedInUser.userid}) OR (userid1 = {LoggedInUser.userid} AND userid2 = {friend.userid})", database);
            delete.Prepare();
            delete.ExecuteNonQuery();
        }
    }

    private static User? GetUserFromEmail(NpgsqlConnection database, string email)
    {
        if (!Util.IsValid(email))
        {
            Console.WriteLine("Invalid email format");
            return null;
        }

        var query = new NpgsqlCommand($"SELECT * FROM \"user\" WHERE email LIKE '{email}'", database);
        var reader = query.ExecuteReader();
        if (reader.Read())
        {
            User user = readerToUser(reader);
            reader.Close();
            return user;
        }

        reader.Close();
        return null;
    }

    /// <summary>
    /// Prompts user for login details
    /// </summary>
    /// <param name="database">database to log into</param>
    public static void LogInPrompt(NpgsqlConnection database)
    {
        var username = Util.GetInput("Username: ");
        var password = Util.GetInput("Password: ");
        // todo hash password asap
        LogIn(database, username, password);
    }

    /// <summary>
    /// Login to the Database
    /// </summary>
    /// <param name="database">database to log into</param>
    /// <param name="username">Username to login</param>
    /// <param name="password">Password to login</param>
    private static void LogIn(NpgsqlConnection database, string username, string password)
    {
       
        // Get user from DB
        var cmd = new NpgsqlCommand($"SELECT * FROM \"user\" WHERE username LIKE '{username}'", database);
        var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if ((string)reader["password"] == password)
            {
                LoggedInUser = readerToUser(reader);
                reader.Close();

                using var insert = new NpgsqlCommand($"UPDATE \"user\" SET lastaccessed = ($1) WHERE username = '{username}'", database)
                {
                    Parameters = {
                        new() { Value = DateTime.Now },
                    }
                };
                insert.Prepare();
                insert.ExecuteNonQuery();
                Console.WriteLine($"[SERVER] | Welcome {username}!");
                return;
            }
        }
        reader.Close();
        Console.WriteLine($"[SERVER] | Incorrect Username or Password, unable to login");
    }
    
    
    /// <summary>
    /// Prompts user for new user details
    /// </summary>
    /// <param name="database">database to query</param>
    public static void CreateUserPrompt(NpgsqlConnection database)
    {
        // Accept only valid email
        string email;
        for (;;)
        {
            email = Util.GetInput("Email: ");
            // break if valid
            if(Util.IsValid(email))
                break;
            Console.WriteLine("[SERVER] | Please enter a valid email");
        }
        
    
        // Accept only unique id
        string username;
        for (;;)
        {
            username = Util.GetInput("Username: ");
            // Check if unique
            if (Util.IsUniqueUsername(database, username))
                break;
            Console.WriteLine($"[SERVER] | Sorry, username \"{username}\" is taken, please try again");
        }

        // Get full name
        var firstName = Util.GetInput("First Name: ");
        var lastName = Util.GetInput("Last Name: ");
        
        // Accept proper dob string
        DateOnly dob;
        for (;;)
        {
            var dobInput = Util.GetInput("Date of Birth (MM/DD/YYYY): ");
            try
            {
                var dobParts = dobInput.Split("/");
                dob = new DateOnly(int.Parse(dobParts[2]), int.Parse(dobParts[1]), int.Parse(dobParts[0]));
            }
            catch (Exception e)
            {
                // dob failed
                Console.WriteLine("[SERVER] | Couldn't parse Date of Birth, please format in the following form: (MM/DD/YYYY)");
                continue;
            }
            // dob success
            break;
        }
        var password = Util.GetInput("Password: ");
        // todo salt immediately
        
        // attempt to create user
        if (CreateUser(database, email, username, firstName, lastName, dob, password))
            LogIn(database, username, password);    // login on success
        else
            Console.WriteLine($"[SERVER] | Failed to create user {username}");  // report failure
    }

    /// <summary>
    /// Create new User inside Database
    /// </summary>
    /// <param name="database">Database to query</param>
    /// <param name="email">User's email</param>
    /// <param name="username">User's unique username</param>
    /// <param name="firstName">User's first name</param>
    /// <param name="lastName">User's last name</param>
    /// <param name="dob">User's date of birth</param>
    /// <param name="password">User's hashed and salted password</param>
    /// <returns>true if succeed, false otherwise</returns>
    private static bool CreateUser(NpgsqlConnection database, string email, string username, string firstName, string lastName, DateOnly dob, string password)
    {
        // quick validate args
        if (email.Length <= 0 || 
            !Util.IsValid(email) || 
            username.Length <= 0 || 
            password.Length <= 0 ||
            firstName.Length <= 0 ||
            lastName.Length <= 0 ||
            !Util.IsUniqueUsername(database, username)) 
            return false;
        
        // Args are valid, insert into table
        using var insert = new NpgsqlCommand("INSERT INTO \"user\"(email, username, firstname, lastname, dob, creationdate, lastaccessed, password) VALUES($1, $2, $3, $4, $5, $6, $7, $8)", database)
        {
            Parameters =
            {
                new() { Value = email },
                new() { Value = username },
                new() { Value = firstName },
                new() { Value = lastName },
                new() { Value = dob },
                new() { Value = DateTime.Now }, // creation date is right now
                new() { Value = DateTime.Now }, // last accessed date is right now
                new() { Value = password }
            }
        };
        insert.Prepare();
        var inserted = insert.ExecuteNonQuery();
        return inserted >= 0;   // success if added
    }
}