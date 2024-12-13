using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;

namespace LaunchBoxCleanUp
{
    internal class AuditParser
    {
        private List<string> _errorMessages = new List<string>();

        internal List<string> ErrorMessages
        {
            get { return _errorMessages; }
        }

        private string[] _delimeter = new string[] { Const.DEFAULT_DELIMETER };

        internal string[] Delimeter
        {
            get { return _delimeter; }
            set { _delimeter = value; }
        }

        private string _fileName;

        internal string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(this._fileName))
                {
                    throw new AccessViolationException("Please set the filename first!");
                }

                return _fileName;
            }
            set { _fileName = value; }
        }

        internal string[] Headers { get; private set; }
        internal List<GameEntry> GameEntries { get; private set; }

        internal AuditParser()
        {
            Headers = new string[0];
            GameEntries = new List<GameEntry>();
        }

        internal List<string> GetDistinctIDs()
        {
            return GameEntries.Select(o => o.GameId)
                .Where(o => !string.IsNullOrEmpty(o.Trim()))
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        internal List<string> GetFilterOptions()
        {
            return GameEntries.SelectMany(o => o.FilterOptions)
                .Where(o => !string.IsNullOrEmpty(o.Trim()))
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        internal List<GameEntry> GetGamesById(String id)
        {
            return GameEntries.FindAll(o => o.GameId.ToLower().Equals(id.ToLower()));
        }

        internal bool ReadData()
        {
            bool ret = false;

            try
            {
                if (File.Exists(FileName))
                {
                    using (TextFieldParser parser = new TextFieldParser(FileName))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.Delimiters = Delimeter;
                        parser.HasFieldsEnclosedInQuotes = false;
                        parser.TrimWhiteSpace = true;
                        int lineNr = 0;

                        while (parser.PeekChars(1) != null)
                        {
                            lineNr++;

                            var cleanFieldRowCells = parser.ReadFields().Select(
                                f => f.Trim(new[] { ' ', '"' })).ToList();

                            if ((null == cleanFieldRowCells || cleanFieldRowCells.Count <= 1))
                            {
                                throw new InvalidOperationException("The file does not contain valid LaunchBox Audit data.");
                            }

                            if (cleanFieldRowCells.Count == 43)
                                cleanFieldRowCells.Insert(0, "mspltfrm");

                            if (lineNr == 1)
                            {
                                if (cleanFieldRowCells.Contains(Const.LAUNCHBOX_DATABASE_ID_HEADER_NAME) && cleanFieldRowCells[cleanFieldRowCells.Count - 1].Equals(Const.LAUNCHBOX_DATABASE_ID_HEADER_NAME))
                                {
                                    Headers = cleanFieldRowCells.ToArray();
                                }
                                else
                                {
                                    throw new InvalidOperationException("The file does not contain valid LaunchBox Audit data.");
                                }
                            }
                            else
                            {
                                if (cleanFieldRowCells.Count.Equals(Headers.Length))
                                {
                                    GameEntry newGame = new GameEntry(cleanFieldRowCells.ToArray());
                                    GameEntries.Add(newGame);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Malformatted content at line: " + lineNr);
                                }
                            }
                        }

                        ret = (null != Headers && Headers.Length > 0);
                    }
                }
                else
                {
                    throw new FileNotFoundException("File does not existst: " + FileName);
                }
            }
            catch (Exception ex)
            {
                ErrorMessages.Add(ex.Message);
            }

            return ret;
        }

    }
}
