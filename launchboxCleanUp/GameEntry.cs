using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LaunchBoxCleanUp
{
    internal class GameEntry
    {
        public string GameId { get; private set; }
        public string[] Fields { get; private set; }

        public List<string> FilterOptions { get; private set; }
        /*public string DBId
        {
            get { return Fields[Fields.Length - 1]; }
        }*/
        public string Title
        {
            get { return Fields[1]; }
        }
        public string RomPath
        {
            get { return Fields[4]; }
        }

        internal GameEntry(string[] fields)
        {
            if (null != fields && fields.Length > 0)
            {
                Fields = fields;
                GameId = fields[fields.Length - 1];

                if (!string.IsNullOrEmpty(fields[1]) && string.IsNullOrEmpty(fields[fields.Length - 1]))
                {
                    GameId = fields[1]; // Replace the empty DB Id with the Game Title
                }

                if (string.IsNullOrEmpty(GameId))
                {
                    throw new ArgumentException("a valid game id is required!");
                }
            }
            else
            {
                throw new ArgumentException("Game data fields are required!");
            }
            SetFilterOptions();
        }

        private void SetFilterOptions()
        {
            FilterOptions = (Fields[11].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Concat(
                    Fields[21].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                )
                .Concat(
                    (Regex.Split(Fields[19], @"\((.*?)\)", RegexOptions.IgnoreCase)).SelectMany(o => o.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                )
                .Select(x => Regex.Replace(x, @"[\(\)]", "").Trim().ToLower()) //.Select(x => Regex.Replace(x, @"[\[\]\(\)]", "").Trim().ToLower())
                .Where(o => !string.IsNullOrEmpty(o.Trim()))
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", GameId, Title);
        }
    }
}
