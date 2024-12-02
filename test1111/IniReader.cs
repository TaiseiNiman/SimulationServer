using System;
using System.Collections.Generic;
using System.IO;

namespace test1111
{
    public class IniReader
    {
        private Dictionary<string, Dictionary<string, string>> iniData;

        public IniReader()
        {
            iniData = new Dictionary<string, Dictionary<string, string>>();
        }

        public bool LoadIniFile(string fileName)
        {

            if (!File.Exists(fileName))
            {
                Console.WriteLine("INIファイルが見つかりません");
                return false;
            }

            string iniFileContent = File.ReadAllText(fileName);

            using (StringReader reader = new StringReader(iniFileContent))
            {
                string section = null;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith(";"))
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line.Substring(1, line.Length - 2);
                        if (!iniData.ContainsKey(section))
                        {
                            iniData[section] = new Dictionary<string, string>();
                        }
                    }
                    else if (section != null)
                    {
                        string[] keyValue = line.Split('=');
                        if (keyValue.Length == 2)
                        {
                            iniData[section][keyValue[0].Trim()] = keyValue[1].Trim();
                        }
                    }
                }
            }
            return true;
        }

        public string GetIniValue(string section, string key)
        {
            if (iniData.ContainsKey(section) && iniData[section].ContainsKey(key))
            {
                return iniData[section][key];
            }
            return null;
        }
    }
}