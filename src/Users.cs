using System.Security.Cryptography;
using System.Text;
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
                case "create account":
                    CreateAccountPrompt(database);
                    break;
                case "login":
                    LoginPrompt(database);
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

    private static void CreateAccountPrompt(NpgsqlConnection database)
    {
        Console.WriteLine("Enter your email");
        var email = Console.ReadLine();
        Console.WriteLine("Enter your username");
        var username = Console.ReadLine();
        Console.WriteLine("Enter your first name");
        var firstName = Console.ReadLine();
        Console.WriteLine("Enter your last name");
        var lastName = Console.ReadLine();
        Console.WriteLine("Enter the year you were born");
        var dob_year = int.Parse(Console.ReadLine());
        Console.WriteLine("Enter the month you were born");
        var dob_month = int.Parse(Console.ReadLine());
        Console.WriteLine("Enter the day you were born");
        var dob_day = int.Parse(Console.ReadLine());
        Console.WriteLine("Enter a password");
        var password = Console.ReadLine();
        DateOnly dob = new DateOnly(dob_year, dob_month, dob_day);
        // ignoring these warnings for velocity sake
        if (CreateUser(database, email, username, firstName, lastName, dob, password))
        {
            Console.WriteLine($"Successfully created new user {username}");
        }
        else
        {
            Console.WriteLine($"Failed to create user {username}");
        }
    }

    private static void LoginPrompt(NpgsqlConnection database)
    {
        Console.WriteLine("Enter your username");
        var username = Console.ReadLine();
        Console.WriteLine("Enter your password");
        var password = Console.ReadLine();
        if (LogIn(database, username, password))
        {
            Console.WriteLine($"Logged in as {username}");
        }
        else
        {
            Console.WriteLine($"Failed to log in as {username}");
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
    /// Create a SHA256 hash of a given password using a SALT
    /// </summary>
    /// <param name="password">Password to salt and hash</param>
    /// <param name="username">unique id to use for salt</param>
    /// <returns>SHA256 string of salted password</returns>
    private static string toSaltedHash(string password, string username)
    {
        // salt w/ username since unique
        var salt = "thereisasus" + username + "amongus";
        
        // Semi randomly break apart password and insert a salt character
        foreach (var c in salt)
        {
            var insertIndex = c % password.Length;
            // Break into 2 sides
            var left = password.Substring(0, insertIndex);
            var right = password.Substring(insertIndex, password.Length - insertIndex);
            
            // update password
            password = left + c + right;
        }
        // hash salted password
        using var hash = SHA256.Create();
        var byteArray = hash.ComputeHash(Encoding.UTF8.GetBytes(password));
        // return SHA256 string
        return Convert.ToHexString(byteArray);
    }

    public static bool LogIn(NpgsqlConnection database, string username, string password)
    {
        var cmd = new NpgsqlCommand($"SELECT * FROM \"user\" WHERE username LIKE '{username}'", database);
        var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            // convert to salted hash and then compare
            if ((string)reader["password"] == toSaltedHash(password, username))
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

                return true;
            }
        }

        reader.Close();

        return false;
    }

    public static bool CreateUser(NpgsqlConnection database, string email, string username, string firstName, string lastName, DateOnly dob, string password)
    {

        // Checking inputs for validity
        // Checking for valid email
        if (email.Length > 0 && Util.IsValid(email) && username.Length > 0 && password.Length > 0)
        {
            // Checking for unique username
            var cmd = new NpgsqlCommand($"SELECT * FROM \"user\" WHERE username LIKE '{username}'", database);
            var reader = cmd.ExecuteReader();

            // Checking for long enough first and last names
            if (reader.Rows == 0 && firstName.Length > 0 && lastName.Length > 0)
            {
                reader.Close();
                // Checking for long enough password
                
                // SALT and Hash password
                password = toSaltedHash(password, username);

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

                if (inserted >= 0)
                {
                    reader.Close();
                    return true;
                }
            }

            reader.Close();
        }
        return false;
    }
}