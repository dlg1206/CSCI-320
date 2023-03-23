using Npgsql;

struct User
{
    public User(int userid, string email, string username, string firstName, string lastName, DateTime dob, DateTime creationDate, DateTime lastAccessed, string pass)
    {
        this.userid = userid;
        this.email = email;
        this.username = username;
        this.firstName = firstName;
        this.lastName = lastName;
        this.dob = dob;
        this.creationDate = creationDate;
        this.lastAccessed = lastAccessed;
        this.password = pass;
    }
    public int userid;
    public string email;
    public string username;
    public string firstName;
    public string lastName;
    public string password;
    public DateTime dob;
    public DateTime creationDate;
    public DateTime lastAccessed;
}


class Users
{
    public static User LoggedInUser { get; private set; }

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

    public static void CreateUser(NpgsqlConnection database, string email, string username, string firstName, string lastName, DateOnly dob, string password)
    {
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
        insert.ExecuteNonQuery();
    }
}