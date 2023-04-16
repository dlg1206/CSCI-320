using System.Text.RegularExpressions;
using Npgsql;

class Util
{
    // list of commands to use
    private static List<string>? _commands;

    private const string ServerName = "spotify2";
    public static string userName { get; set; } = "guest";
    
    public static void InitPresetCommands(IEnumerable<string> cmdFiles)
    {
        _commands = new List<string>();
        // if the cmd file exists, split the file on the newline and add the commands
        foreach (var cmd in cmdFiles.Where(File.Exists).SelectMany(cmdFile => File.ReadAllText(cmdFile).Split(Environment.NewLine)))
        {
            _commands.Add(cmd);
        }
    }

    public static string GetServerPrompt(string dir="")
    {
        return $"{userName}@{ServerName}:~{dir}$ ";
    }

    public static string ServerMessage(string msg)
    {
        return $"[SERVER] | {msg}";
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
        string? devInput = null;
        for (;;)
        {
            // display prompt if present
            if(prompt != null) Console.Write(prompt);
            
            // check if preset commands are present
            if (_commands != null && _commands.Count != 0 && devInput == null)
            {
                 devInput = _commands[0];    // get command
                _commands.RemoveAt(0);          // pop list
                
                // if not special prompt cmd, write cmd and continue
                if (!devInput.Equals("$PROMPT"))
                {
                    Console.WriteLine(devInput);
                    return devInput;
                }
            }
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