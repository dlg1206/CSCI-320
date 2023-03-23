using System.Text.RegularExpressions;

class Util
{
    public static bool IsValid(string email)
    { 
        string regex = @"^[^@\s]+@[^@\s]+\.(com|net|org|gov)$";

        return Regex.IsMatch(email, regex, RegexOptions.IgnoreCase);
    }
}