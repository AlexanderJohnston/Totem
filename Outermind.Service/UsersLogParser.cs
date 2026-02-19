using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Quantum.ServiceContracts;
using Quantum.SmartScanning;

namespace Quantum.Service
{
  public class UsersLogParser : IUsersLogParser
  {
    private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");

    public List<UserEvent> ParseFile(string filePath)
    {
      if (!File.Exists(filePath))
        return new List<UserEvent>();
      var lines = File.ReadLines(filePath);
      return ParseLines(lines);
    }

    public List<UserEvent> ParseLines(IEnumerable<string> lines)
    {
      var result = new List<UserEvent>();

      foreach (var raw in lines)
      {
        if (string.IsNullOrWhiteSpace(raw))
          continue;

        // Split by comma. The sample format is unquoted CSV; paths and tokens contain spaces but not commas.
        var tokens = raw.Split(',')
                        .Select(t => t.Trim())
                        .ToArray();

        if (tokens.Length < 3)
          continue;

        string user = tokens[0];
        string actionField = tokens[1];
        DateTime ts;

        if (!DateTime.TryParse(tokens[2], UsCulture, DateTimeStyles.AssumeLocal, out ts))
        {
          continue;
        }

        string source = tokens.Length >= 4 ? tokens[3] : "";
        string machine = tokens.Length >= 5 ? tokens[4] : "";

        var ev = new UserEvent
        {
          User = user,
          Action = actionField,
          Timestamp = ts,
          Source = source,
          Machine = machine
        };

        if (StartsWithWord(actionField, "open"))
        {
          ev.Action = "open";
          ev.Path = ExtractAfterFirstSpace(actionField);
        }
        else if (StartsWithWord(actionField, "close"))
        {
          ev.Action = "close";
          ev.Path = ExtractAfterFirstSpace(actionField);
        }

        if (tokens.Length > 5)
        {
          for (int i = 5; i < tokens.Length; i++)
            ev.Details.Add(tokens[i]);

          if (ev.Path == null)
          {
            var maybePath = tokens.Last();
            if (LooksLikePath(maybePath))
              ev.Path = maybePath;
          }
        }

        result.Add(ev);
      }

      return result;
    }

    private static bool StartsWithWord(string s, string word)
        => s.StartsWith(word + " ", StringComparison.OrdinalIgnoreCase) || s.Equals(word, StringComparison.OrdinalIgnoreCase);

    private static string ExtractAfterFirstSpace(string s)
    {
      int idx = s.IndexOf(' ');
      if (idx < 0 || idx + 1 >= s.Length)
        return null;
      return s.Substring(idx + 1).Trim();
    }

    private static bool LooksLikePath(string s)
    {
      if (string.IsNullOrEmpty(s)) return false;
      if (s.Length >= 3 && char.IsLetter(s[0]) && s[1] == ':' && (s[2] == '\\' || s[2] == '/')) return true;
      if (s.StartsWith(@"\\", StringComparison.Ordinal)) return true;
      if (s.Contains('\\') && s.Contains('.')) return true;
      return false;
    }
  }
}

