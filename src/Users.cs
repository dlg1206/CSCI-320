using Npgsql;

record User(int userid, string email, string username, string firstName, string lastName, DateTime dob, DateTime creationDate, DateTime lastAccessed, string password);

class Users
{
    public static User? LoggedInUser { get; private set; } = null;

    /// <summary>
    /// Handles commands for User
    /// </summary>
    /// <param name="database">database to use</param>
    /// <param name="args">cli args</param>
    /// <returns>true if successful</returns>
    public static bool HandleInput(NpgsqlConnection database, string[] args)
    {
        // switch through keywords
        switch (args[0].ToLower())
        {
            case "login":
                return LogInPrompt(database);
            case "new":
                return CreateUserPrompt(database);
            case "friends":
                ListFriends(database);
                break;
            case "follow":
                HandleFriend(database, true);
                break;
            case "unfollow":
                HandleFriend(database, false);
                break;
        }
        // unknown arg
        return false;
    }

    
    private static User readerToUser(NpgsqlDataReader reader)
    {
        return new User(
            (int) reader["userid"], 
            (string) reader["email"], 
            (string) reader["username"], 
            (string) reader["firstname"],
            (string) reader["lastname"],
            (DateTime) reader["dob"],
            (DateTime) reader["creationdate"],
            (DateTime) reader["lastaccessed"],
            (string) reader["password"]
            );
    }

    public static User? GetUser(NpgsqlConnection database, int userId)
    {
        var cmd = new NpgsqlCommand($"SELECT * FROM \"user\" WHERE userId={userId}", database);
        var reader = cmd.ExecuteReader();
        User? user;
        try
        {
            reader.Read();
            user = readerToUser(reader);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            user = null;
        }
        finally
        {
            reader.Close();
        }
       
        return user;
    }

    private static void ListFriends(NpgsqlConnection database)
    {
        var query = new NpgsqlCommand($"SELECT * FROM friend WHERE userid1={LoggedInUser!.userid}", database);
        var reader = query.ExecuteReader();
        
        var friendIds = new List<int>();
        while (reader.Read())
        {
            friendIds.Add((int) reader["userid2"]);
        }
        reader.Close();
        if (friendIds.Count == 0)
        {
            Util.ServerMessage("Couldn't find any Friends!");
            return;
        }
        
        Console.WriteLine($"You have {friendIds.Count} friend" + (friendIds.Count == 1 ? "" : "s"));

        var friendCount = 1;
        // for each friend id, if the user exists print user info
        foreach(var id in friendIds)
        {
            var u = GetUser(database, id);
            if(u == null) continue;
            Console.WriteLine($"\tFriend {friendCount++}: {u.username}\t| Last seen: {u.lastAccessed}");
        }
        
        
        

    }

    private static void HandleFriend(NpgsqlConnection database, bool follow)
    {

        var email = Util.GetInput("Enter your friends email: ");
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
    private static bool LogInPrompt(NpgsqlConnection database)
    {
        var username = Util.GetInput("Username: ");
        var password = Util.GetInput("Password: ");
        // todo hash password asap
        // report success
        var loginSuccess = LogIn(database, username, password);
        Console.WriteLine(loginSuccess
            ? $"[SERVER] | Welcome {username}!"
            : "[SERVER] | Incorrect Username or Password, unable to login");
        return loginSuccess;
    }

    /// <summary>
    /// Login to the Database
    /// </summary>
    /// <param name="database">database to log into</param>
    /// <param name="username">Username to login</param>
    /// <param name="password">Password to login</param>
    private static bool LogIn(NpgsqlConnection database, string username, string password)
    {
        // Get user from DB
        var cmd = new NpgsqlCommand($"SELECT * FROM \"user\" WHERE username LIKE '{username}'", database);
        var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            // break if password bad
            // todo number of password attempts?
            if ((string) reader["password"] != password) break;
            
            LoggedInUser = readerToUser(reader);
            reader.Close();
            // update last accessed
            using var insert = new NpgsqlCommand($"UPDATE \"user\" SET lastaccessed = ($1) WHERE username = '{username}'", database)
            {
                Parameters = {
                    new() { Value = DateTime.Now },
                }
            };
            insert.Prepare();
            insert.ExecuteNonQuery();
            return true;
        }
        // username is bad
        reader.Close();
        return false;
    }
    
    
    /// <summary>
    /// Prompts user for new user details
    /// </summary>
    /// <param name="database">database to query</param>
    private static bool CreateUserPrompt(NpgsqlConnection database)
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
            return LogIn(database, username, password);     // attempt login on success
        
        // else report failure
        Console.WriteLine($"[SERVER] | Failed to create user {username}");  
        return false;
        
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