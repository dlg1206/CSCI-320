using Npgsql;

record User(int userid, string email, string username, string firstName, string lastName, DateTime dob, DateTime creationDate, DateTime lastAccessed, string password);

class Users
{
    public static User? LoggedInUser { get; private set; } = null;
    public static void HandleInput(NpgsqlConnection database)
    {
        Console.WriteLine("User input possibilities: create account, login");
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
    private static bool CreateAccountPrompt(NpgsqlConnection database)
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
        return CreateUser(database, email, username, firstName, lastName, dob, password);
    }
    private static bool LoginPrompt(NpgsqlConnection database)
    {
        return false;
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

    public static void AddFriend(NpgsqlConnection database, User friend)
    {
        if (LoggedInUser != null)
        {
            using var insert = new NpgsqlCommand($"INSERT friend(userid1, userid2) VALUES({LoggedInUser?.userid}, {friend.userid})", database);
            insert.Prepare();
            insert.ExecuteNonQuery();
        }
    }

    public static bool LogIn(NpgsqlConnection database, string username, string password)
    {
        var cmd = new NpgsqlCommand($"SELECT * FROM \"user\" WHERE username='{username}'", database);
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
                // Hash password here
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