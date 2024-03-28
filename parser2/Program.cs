using System;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

new Parser("aboba").CountGibleMetric();

public class Parser(string source)
{
    private Dictionary<string, int> _operands = new();
    private Dictionary<string, int> _operators = new();
    public int UniqueOperators { get; private set; }
    public int TotalOperators { get; private set; }
    public int UniqueOperands { get; private set; }
    public int TotalOperands { get; private set; }

    public IReadOnlyDictionary<string, int> Operands => _operands.AsReadOnly();
    public IReadOnlyDictionary<string, int> Operators => _operators.AsReadOnly();


    public Tuple<int, float, int> CountGibleMetric()
    {
        int deepest = FindDeepestNestingOperator();
        int allIfs = CountIfStatements();
        int allCases = CountAllCases();
        int CL = allCases + allIfs;
        int allOperators = 0;
        foreach (var op in Operators)
        {
            if (!(op.Key == "case" || op.Key == "switch" || op.Key == "default" || op.Key == "if" || op.Key == "else" || op.Key == "elif" || op.Key == "elseif"))
            {
                allOperators++;
            }
        }
        allOperators += CL;
        float cl = 0.0f;
        if (allOperators != 0)
        {
            cl = (float)CL / (float)allOperators;
        }
        return Tuple.Create(CL, cl, deepest);
    }

    private int CountCases(string switchMatch)
    {
        var caseRegex = new Regex(@"\b(case|default)\b");
        var matches = caseRegex.Matches(switchMatch);
        return matches.Count;
    }

    private int CountAllCases()
    {
        var switchRegex = new Regex(@"\bswitch\s*\((.*?)\)\s*{((?:\s*(case|default)\s+[^:]+:\s*(?:[^{}]*|(?<DEPTH>{)|(?<-DEPTH>}))*(?(DEPTH)(?!)))+)");
        int totalCases = 0;
        foreach (Match switchMatch in switchRegex.Matches(source))
        {
            string body = switchMatch.Groups[2].Value.Trim();
            totalCases += CountCases(body) - 1;
        }
        return totalCases;
    }

    private int ParseSwitchStatements(string text)
    {
        var switchRegex = new Regex(@"\bswitch\s*\((.*?)\)\s*{((?:\s*(case|default)\s+[^:]+:\s*(?:[^{}]*|(?<DEPTH>{)|(?<-DEPTH>}))*(?(DEPTH)(?!)))+)");
        int maxCountedCases = 0;
        foreach (Match switchMatch in switchRegex.Matches(text))
        {
            string body = switchMatch.Groups[2].Value.Trim();
            maxCountedCases = Math.Max(CountCases(body), maxCountedCases);
        }
        return maxCountedCases;
    }

    private int FindDeepestNestingOperator()
    {
        var regex = new Regex(@"\b(if|switch|elif|else|elseif)\s*\((.*?)\)\s*{((?:(?>[^{}]+)|{(?<DEPTH>)|}(?<-DEPTH>))*(?(DEPTH)(?!)))\}");
        var matches = regex.Matches(source);
        int maxNestingLevel = 0;
        foreach (Match match in matches)
        {
            string operatorType = match.Groups[1].Value;
            string body = match.Groups[3].Value.Trim();
            int nestingLevel = 0;
            if (operatorType == "switch")
            {
                nestingLevel = CountCases(body) - 2;
            }
            else
            {
                nestingLevel = CountNestingLevel(body, 0);
            }

            maxNestingLevel = Math.Max(maxNestingLevel, nestingLevel);
        }

        return maxNestingLevel;
    }

    private int CountNestingLevel(string body, int currentNestingLevel)
    {
        int maxNestingLevel = currentNestingLevel;

        // Find nested if and switch statements
        var nestedRegex = new Regex(@"\b(if|switch|elif|else|elseif)\s*\((.*?)\)\s*{((?:(?>[^{}]+)|{(?<DEPTH>)|}(?<-DEPTH>))*(?(DEPTH)(?!)))\}");
        var nestedMatches = nestedRegex.Matches(body);

        foreach (Match nestedMatch in nestedMatches)
        {
            string operatorType = nestedMatch.Groups[1].Value;
            string nestedBody = nestedMatch.Groups[3].Value.Trim();

            int nestingLevel = CountNestingLevel(nestedBody, currentNestingLevel + 1);
            maxNestingLevel = Math.Max(maxNestingLevel, nestingLevel);
        }

        // Calculate switch depth
        if (body.Contains("switch"))
        {
            int switchDepth = ParseSwitchStatements(body) - 2 + currentNestingLevel;
            maxNestingLevel = Math.Max(maxNestingLevel, switchDepth);
        }

        return maxNestingLevel;
    }

    private int CountIfStatements()
    {
        // Regular expression to match 'if' statements
        string pattern = @"\b(if|else|elif|elseif)\b";
        Regex regex = new Regex(pattern);

        // Find all matches
        MatchCollection matches = regex.Matches(source);

        // Return the count of matches
        return matches.Count;
    }
}