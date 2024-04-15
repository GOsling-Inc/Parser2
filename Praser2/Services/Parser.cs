using System.Text.RegularExpressions;

namespace Services
{
    public class Item(string index, string name, string count)
    {
        public string Index { get; set; } = index;
        public string Name { get; set; } = name;
        public string Count { get; set; } = count;
    }
    public class Parser
    {
        readonly string[] allOperators = [";", "+", "-", "*", "/", "%", "**", "==", "!=", ">", "<", ">=", "<=", "&&", "||", "!", "&", "|", "=", "+=", "-=", "*=", "/=", "%=", "**=", "<<=", ">>=", "&=", "^=", "|=", "^", "<<", ">>", "~", ">>>"];
        static Dictionary<string, int> operators = [];
        static bool hasDefault = false;
        public Parser() { }
        public Tuple<List<Item>, int, int, int, float> CountGilbeMetric(string text)
        {
            int countConditions = CountAllCases(text) + CountAllIf(text) + CountAllLoops(text);
            int allOperators = CountDefaultOperators(text);
            int deepest = FindDeepestNesting(text);
            var ops = new List<Item>
            {
                 new("индекс", "оператор", "количество")
            };
            var newoperators = from entry in operators orderby entry.Value descending select entry;
            for (int i = 0; i < newoperators.Count(); i++)
                if (newoperators.ElementAt(i).Value > 0)
                    ops.Add(new Item((i + 1).ToString(), newoperators.ElementAt(i).Key, newoperators.ElementAt(i).Value.ToString()));
            return Tuple.Create(ops, allOperators, countConditions, deepest, (float)countConditions / allOperators);
        }

        private static int CountCases(string text)
        {
            var caseRegex = new Regex(@"\b(case)\b\s\w");
            var matches = caseRegex.Matches(text);
            var totalCases = matches.Count;
            if (matches[^1].ToString()[^1] == '_') hasDefault = true;
            if (hasDefault) return totalCases;
            return totalCases + 1;
        }

        private static int CountAllCases(string text)
        {
            var matchRegex = new Regex(@"\bmatch\s*{((?:\s*(case)\s+[^=]+[^>+][=][>]\s*(?:(?<DEPTH>{)|(?<-DEPTH>})|[^{}]*)*(?(DEPTH)(?!)))+)");
            int totalCases = 0;
            operators["match"] = 0;
            operators["case"] = 0;
            foreach (Match match in matchRegex.Matches(text))
            {
                operators["match"]++;
                string body = match.Groups[1].Value.Trim();
                totalCases += CountCases(body) - 1;
            }
            operators["case"] = totalCases;
            return totalCases;
        }

        private static int CountAllIf(string text)
        {
            var ifRegex = new Regex(@"(\bif\b|\}\s*else if|\belse\b)");
            var matches = ifRegex.Matches(text);
            operators["if"] = operators["else if"] = operators["else"] = 0;
            foreach(Match match in matches)
            {
                if (match.ToString() == "if" || match.ToString() == "else")
                    operators[match.ToString()]++;
                else operators["else if"]++;
            }
            return matches.Count - operators["else"];
        }

        private static int CountAllLoops(string text)
        {
            var loopRegex = new Regex(@"\b(for|while)\b");
            var matches = loopRegex.Matches(text);
            operators["for"] = operators["while"] = 0;
            foreach(Match match in matches)
            {
                operators[match.ToString()]++;
            }
            return matches.Count;
        }

        private int CountDefaultOperators(string code)
        {
            int count = 0;
            foreach (var item in allOperators)
            {
                var newItem = "";
                foreach (var i in item)
                {
                    newItem += $@"\{i}";
                }
                Regex operatorsReg = new($"[0-9a-zA-Z\r\n ]({newItem})[0-9a-zA-Z\r\n ]");
                var op_matches = operatorsReg.Matches(code);
                count += op_matches.Count;
                if ( op_matches.Count > 0 )
                    operators[item] = op_matches.Count;
            }
            /*Regex point = new(@"[a-zA-Z]+[a-zA-Z0-9_]*(\.)[a-zA-Z]+[a-zA-Z0-9_]*");
            var matches = point.Matches(code);
            count += matches.Count;
            if ( matches.Count > 0 )
                operators["."] = matches.Count;*/
            return count;
        }

        private static int DeepestMatchStatements(string text)
        {
            var matchRegex = new Regex(@"\bmatch\s*{((?:\s*(case)\s+[^=]+[^>+][=][>]\s*(?:(?<DEPTH>{)|(?<-DEPTH>})|[^{}]*)*(?(DEPTH)(?!)))+)");
            int maxCountedCases = 0;
            foreach (Match match in matchRegex.Matches(text))
            {
                string body = match.Groups[2].Value.Trim();
                maxCountedCases = Math.Max(CountMatchNesting(body, maxCountedCases), maxCountedCases);
            }
            return maxCountedCases;
        }

        private static int CountMatchNesting(string text, int currentNestingLevel)
        {
            int maxNestingLevel = currentNestingLevel;
            var caseRegex = new Regex(@"\bcase\s+[^=]+[^>+][=][>]\s*(?:\s*\{((?>(?:(?<DEPTH>{)|(?<-DEPTH>}))|[^{}]+)*(?(DEPTH)(?!)))\}|(.*))");
            var matches = caseRegex.Matches(text);
            int matchLevel = 0;
            foreach (Match match in matches)
            {
                string caseBody = match.Groups[1].Value.Trim();
                if (caseBody.Length == 0)
                    caseBody = match.Groups[2].Value.Trim();
                maxNestingLevel = Math.Max(maxNestingLevel, CountNestingLevel(caseBody, matchLevel + 1));
                if(match != matches[^2] || (!hasDefault && match == matches[^2]))
                    ++matchLevel;
            }
            return maxNestingLevel;
        }

        private static int CountNestingLevel(string body, int currentNestingLevel)
        {
            int maxNestingLevel = currentNestingLevel;

            var nestedRegex = new Regex(@"\b(else if|if|match|else|for|while)\s*\(*(.*?)\)*\s*{((?:{(?<DEPTH>)|(?<-DEPTH>})|(?>[^{}]+))*(?(DEPTH)(?!)))\}");
            var nestedMatches = nestedRegex.Matches(body);

            if (nestedMatches.Count == 0) return 0;

            foreach (Match nestedMatch in nestedMatches)
            {
                string nestedBody = nestedMatch.Groups[3].Value.Trim();

                int nestingLevel = CountNestingLevel(nestedBody, currentNestingLevel + 1);
                maxNestingLevel = Math.Max(maxNestingLevel, nestingLevel);
            }

            if (body.Contains("match"))
            {
                int switchDepth = CountCases(body) + DeepestMatchStatements(body) - 2 + currentNestingLevel;
                maxNestingLevel = Math.Max(maxNestingLevel, switchDepth);
            }

            return maxNestingLevel;
        }

        private static int FindDeepestNesting(string text)
        {
            var regex = new Regex(@"\b(else if|if|match|else|for|while)\s*\(*(.*?)\)*\s*{((?:{(?<DEPTH>)|(?<-DEPTH>})|(?>[^{}]+))*(?(DEPTH)(?!)))\}");
            var matches = regex.Matches(text);
            int maxNestingLevel = 0;
            foreach (Match match in matches)
            {
                string operatorType = match.Groups[1].Value;
                string body = match.Groups[3].Value.Trim();
                int nestingLevel = 0;
                if (operatorType == "match")
                    nestingLevel = CountMatchNesting(body, CountCases(body) - 2);
                else
                    nestingLevel = CountNestingLevel(body, 1);

                maxNestingLevel = Math.Max(maxNestingLevel, nestingLevel);
            }

            return maxNestingLevel;
        }

    }
}
