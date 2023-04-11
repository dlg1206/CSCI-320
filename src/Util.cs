using System.Text.RegularExpressions;
using Npgsql;

class Util
{
    public const string ServerName = "spotify2";
    public static string UserName { get; set; } = "guest";

    public static string GetServerPrompt(string dir="")
    {
        return $"{UserName}@{ServerName}:~{dir}$ ";
    }
    public static bool IsValid(string email)
    { 
        string regex = @"^[^@\s]+@[^@\s]+\.(com|net|org|gov)$";

        return Regex.IsMatch(email, regex, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Utility get input until get response
    /// </summary>
    /// <param name="prompt">optional prompt text</param>
    /// <returns>string of input</returns>
    public static string GetInput(string? prompt=null)
    {
        // repeat until input is good
        // todo put cap?
        for (;;)
        {
            // display prompt if present
            if(prompt != null) Console.Write(prompt);
            var input = Console.ReadLine();

            // return input if good
            if (!string.IsNullOrEmpty(input)) return input;
        }
    }

    /// <summary>
    /// Utility check if given username is unique in the targe database
    /// </summary>
    /// <param name="database">database to query</param>
    /// <param name="username">username</param>
    /// <returns>true if unique, false otherwise</returns>
    public static bool IsUniqueUsername(NpgsqlConnection database, string username)
    {
        var cmd = new NpgsqlCommand($"SELECT * FROM \"user\" WHERE username LIKE '{username}'", database);
        var reader = cmd.ExecuteReader();
        var result = !reader.HasRows;   // if has rows than username exists
        reader.Close();
        return result;
    }
}