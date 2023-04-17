using System.Data;
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
            // handle login
            case "login":
                return LogInPrompt(database);
            
            // handle new user
            case "new":
                return CreateUserPrompt(database);
            
            // handle follow commands
            case "list":
            case "follow":
            case "unfollow":
                if(args.Length > 1)
                    HandleFollowAction(database, args[0], args[1]);
                else
                    HandleFollowAction(database, args[0]);
                break;
            case "top":
                ListTopTenArtists(database);
                break;
                
        }
        // unknown arg
        return false;
    }
    
    /// <summary>
    /// Handles the follow actions
    /// </summary>
    /// <param name="database">db to query</param>
    /// <param name="command">follow command to execute</param>
    /// <param name="arg">optional argument for command</param>
    private static void HandleFollowAction(NpgsqlConnection database, string command, string? arg=null)
    {
        // switch based on the follow command
        switch (command)
        {
            // list command
            case "list":
                // list users based on arg
                if (arg != null)
                {
                    ListUsers(database, arg);
                }
                // else list both follows and follwers
                else
                {
                    ListUsers(database, "follows");
                    ListUsers(database, "followers");
                }
                break;
            
            case "follow":
            case "unfollow":
                // get user by username
                var username = arg ?? Util.GetInput("Enter username: ");    // prompt if no name given
                var user = GetUserByUsername(database, username);
                
                // check if user exists
                if (user == null)
                {
                    Util.ServerMessage($"User \"{username}\" does not exist!");
                    break;
                }
                // else follow or unfollow
                if (command.Equals("follow"))
                    Follow(database, user);
                else
                    Unfollow(database, user);
                break;
        }
        
    }

    
    /// <summary>
    /// List a user's top artists
    /// </summary>
    /// <param name="database">db to query</param>
    private static void ListTopTenArtists(NpgsqlConnection database)
    {
        // get top 10 artist ids based on song count
        var cmd = new NpgsqlCommand(
            $"SELECT artistid FROM ( SELECT songid, timestamp FROM listen WHERE userid={LoggedInUser!.userid} ) as userSongs INNER JOIN artistsong ON userSongs.songid = artistsong.songid GROUP BY artistid, artistsong.songid ORDER BY count(timestamp) desc LIMIT 10;",
            database);
        var reader = cmd.ExecuteReader();
        var artistIds = new List<int>();
        while (reader.Read())
        {
            artistIds.Add((int) reader["artistid"]);
        }
        reader.Close();

        Console.WriteLine("-= Your Top Ten Artists =-");
        var artistCount = 1;
        // get each artist from db based on id
        foreach (var artist in artistIds.Select(id => Artists.GetArtistById(database, id)))
        {
            Console.WriteLine($"{artistCount++}: {artist.name}");
        }
    }
    
    /// <summary>
    /// Utility to convert a reader value to user
    /// </summary>
    /// <param name="reader">reader with data</param>
    /// <returns>New User with reader data</returns>
    private static User ReaderToUser(IDataRecord reader)
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

    /// <summary>
    /// Get a user from the db by id
    /// </summary>
    /// <param name="database">db to query</param>
    /// <param name="userId">user to search for</param>
    /// <returns>User if exists</returns>
    private static User? GetUserById(NpgsqlConnection database, int userId)
    {
        // Prepare query
        var cmd = new NpgsqlCommand($"SELECT * FROM \"user\" WHERE userId={userId}", database);
        var reader = cmd.ExecuteReader();
        User? user;
        try
        {
            // attempt to get user
            reader.Read();
            user = ReaderToUser(reader);
        }
        catch (Exception)
        {
            // error getting user
            user = null;
        }
        finally
        {
            reader.Close();
        }
        return user;
    }
    
    /// <summary>
    /// Get user by username
    /// </summary>
    /// <param name="database">db to query</param>
    /// <param name="username">username to search for</param>
    /// <returns>user if they exist, null otherwise</returns>
    private static User? GetUserByUsername(NpgsqlConnection database, string username)
    {
        // build query
        var query = new NpgsqlCommand($"SELECT * FROM \"user\" WHERE username LIKE '{username}'", database);
        var reader = query.ExecuteReader();

        User? user = null;
        // attempt to get user
        if (reader.Read())
            user = ReaderToUser(reader);
        reader.Close();
        // return result
        return user;
    }

    /// <summary>
    /// List the logged in users followers or who they follow
    /// </summary>
    /// <param name="database">db to query</param>
    /// <param name="relationship"></param>
    private static void ListUsers(NpgsqlConnection database, string relationship)
    {
        NpgsqlCommand query;
        string userIdCol;
        switch (relationship)
        {
            // Get user's followers
            case "followers":
                query = new NpgsqlCommand($"SELECT * FROM friend WHERE userid2={LoggedInUser!.userid}", database);
                userIdCol = "1";
                break;
            // Get who follows the user
            case "follows":
                query = new NpgsqlCommand($"SELECT * FROM friend WHERE userid1={LoggedInUser!.userid}", database);
                userIdCol = "2";
                break;
            default:
                Util.ServerMessage($"\"{relationship}\" is not a valid list command!");
                return;
        }
        // Get user's friends
        var reader = query.ExecuteReader();
        
        // Get all friend ids
        var userIds = new List<int>();
        while (reader.Read())
            userIds.Add((int) reader[$"userid{userIdCol}"]);
        
        reader.Close();
        
        // Check to see if user has followers / follows
        if (userIds.Count == 0)
        {
            Util.ServerMessage($"Couldn't find any {relationship}!");
            return;
        }
        
        // List total followers / follows count
        Console.WriteLine($"You have {userIds.Count} {relationship.Remove(relationship.Length - 1)}" + (userIds.Count == 1 ? "" : "s"));

        // for each user id, if the user exists print user info
        var userCount = 1;
        foreach (var u in userIds.Select(id => GetUserById(database, id)).Where(u => u != null))
        {
            Console.WriteLine($"\tUser {userCount++}: {u.username}\t| Last seen: {u.lastAccessed}");
        }
    }

    private static void Follow(NpgsqlConnection database, User friend)
    {
        if (LoggedInUser != null)
        {
            var insert = new NpgsqlCommand($"INSERT INTO friend(userid1, userid2) VALUES({LoggedInUser?.userid}, {friend.userid})", database);
            insert.Prepare();
            insert.ExecuteNonQuery();
        }
    }

    private static void Unfollow(NpgsqlConnection database, User friend)
    {
        if (LoggedInUser != null)
        {
            var delete = new NpgsqlCommand($"DELETE FROM friend WHERE (userid1 = {friend.userid} AND userid2 = {LoggedInUser.userid}) OR (userid1 = {LoggedInUser.userid} AND userid2 = {friend.userid})", database);
            delete.Prepare();
            delete.ExecuteNonQuery();
        }
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
            
            LoggedInUser = ReaderToUser(reader);
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