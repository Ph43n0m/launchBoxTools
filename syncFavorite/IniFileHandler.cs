using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace syncFavorite
{
    internal class IniFileHandler
    {
        private string _filePath;

        /// <summary>
        /// Initializes a new instance of the IniFileHandler class with the specified file path.
        /// Ensures the file exists.
        /// </summary>
        /// <param name="filePath">Path to the INI file.</param>
        internal IniFileHandler(string filePath)
        {
            _filePath = filePath;
            EnsureFileExists();
        }

        /// <summary>
        /// Ensures the INI file exists, creating it if necessary.
        /// </summary>
        private void EnsureFileExists()
        {
            if (!File.Exists(_filePath))
            {
                File.Create(_filePath).Dispose();
            }
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(
            string section, string key, string defaultValue, StringBuilder returnValue, int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern bool WritePrivateProfileString(
            string section, string key, string value, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue,
            [In, Out] char[] value, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetPrivateProfileSection(string section, IntPtr keyValue,
            int size, string filePath);



        /// <summary>
        /// Reads a value from the INI file for a given section and key.
        /// </summary>
        /// <param name="section">The section in the INI file.</param>
        /// <param name="key">The key within the section.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The value associated with the specified section and key, or the default value if not found.</returns>
        internal string ReadValue(string section, string key, string defaultValue = "")
        {
            var returnValue = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, returnValue, returnValue.Capacity, _filePath);
            return returnValue.ToString();
        }

        /// <summary>
        /// Writes a value to the INI file for a given section and key.
        /// Updates the value if it already exists.
        /// </summary>
        /// <param name="section">The section in the INI file.</param>
        /// <param name="key">The key within the section.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>True if the operation was successful, otherwise false.</returns>
        internal bool WriteValue(string section, string key, string value)
        {
            string existingValue = ReadValue(section, key);
            if (existingValue == value)
            {
                return true; // The value is already up-to-date
            }

            return WritePrivateProfileString(section, key, value, _filePath);
        }

        /// <summary>
        /// Deletes an entire section from the INI file.
        /// </summary>
        /// <param name="section">The section to delete.</param>
        /// <returns>True if the operation was successful, otherwise false.</returns>
        internal bool DeleteSection(string section)
        {
            return WritePrivateProfileString(section, null, null, _filePath);
        }

        private int capacity = 512;

        private string[] ReadKeys(string section, string filePath)
        {
            // first line will not recognize if ini file is saved in UTF-8 with BOM
            while (true)
            {
                char[] chars = new char[capacity];
                int size = GetPrivateProfileString(section, null, "", chars, capacity, filePath);

                if (size == 0)
                {
                    return null;
                }

                if (size < capacity - 2)
                {
                    string result = new String(chars, 0, size);
                    string[] keys = result.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                    return keys;
                }

                capacity = capacity * 2;
            }
        }

        private string[] ReadKeyValuePairs(string section)
        {
            while (true)
            {
                IntPtr returnedString = Marshal.AllocCoTaskMem(capacity * sizeof(char));
                int size = GetPrivateProfileSection(section, returnedString, capacity, _filePath);

                if (size == 0)
                {
                    Marshal.FreeCoTaskMem(returnedString);
                    return null;
                }

                if (size < capacity - 2)
                {
                    string result = Marshal.PtrToStringAuto(returnedString, size - 1);
                    Marshal.FreeCoTaskMem(returnedString);
                    string[] keyValuePairs = result.Split('\0');
                    return keyValuePairs;
                }

                Marshal.FreeCoTaskMem(returnedString);
                capacity = capacity * 2;
            }
        }

        /// <summary>
        /// Retrieves all key-value pairs from a specified section in the INI file.
        /// </summary>
        /// <param name="section">The section to retrieve values from.</param>
        /// <returns>A dictionary containing all key-value pairs in the section.</returns>
        public Dictionary<string, string> GetAllValues(string section)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            string[] kvs = ReadKeyValuePairs(section);
            foreach (string kv in kvs)
            {
                ret.Add(kv.Split('=')[0].Trim(), kv.Split('=')[1].Trim());
            }

            return ret;
        }
    }

}
