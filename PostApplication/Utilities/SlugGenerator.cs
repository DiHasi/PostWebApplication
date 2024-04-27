using System.Text;

namespace PostApplication.Utilities;

public class SlugGenerator
{
    private static readonly Dictionary<string, string> TranslitTable = new()
    {
        {"а", "a"}, {"б", "b"}, {"в", "v"}, {"г", "g"}, {"д", "d"}, {"е", "e"}, {"ё", "yo"}, {"ж", "zh"},
        {"з", "z"}, {"и", "i"}, {"й", "y"}, {"к", "k"}, {"л", "l"}, {"м", "m"}, {"н", "n"}, {"о", "o"},
        {"п", "p"}, {"р", "r"}, {"с", "s"}, {"т", "t"}, {"у", "u"}, {"ф", "f"}, {"х", "h"}, {"ц", "ts"},
        {"ч", "ch"}, {"ш", "sh"}, {"щ", "sch"}, {"ъ", ""}, {"ы", "y"}, {"ь", ""}, {"э", "e"}, {"ю", "yu"},
        {"я", "ya"}, {" ", "-"}
    };

    public static string Generate(string input)
    {
        input = input.ToLowerInvariant();

        var result = new StringBuilder();
        foreach (var ch in input)
        {
            var strCh = ch.ToString();
            if (TranslitTable.TryGetValue(strCh, out var value))
            {
                result.Append(value);
            }
            else if (char.IsLetterOrDigit(ch) && !char.IsPunctuation(ch))
            {
                result.Append(ch);
            }
        }

        return result.ToString();
    }
}