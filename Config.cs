using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SOTSEdit
{
    class ConfigItem
    {
        public ConfigItem()
        {
            friendlyName = "";
            serializedName = "";
            type = typeof(Nullable);
            bytes = 0;
            editable = true;
            hidden = false;
        }

        public string friendlyName;     //friendly name to show in GUI
        public string serializedName;   //text which denotes this item in the save file
        public System.Type type;        //data type. e.g. short
        public int bytes;               //only for null types
        public bool editable;           //should the user edit this value?
        public bool hidden;             //some values shouldn't be shown, but should be parsed.
    }

    class ConfigSection
    {
        public ConfigSection()
        {
            items = new List<ConfigItem>();
            hidden = false;
        }

        public string sectionName;     //friendly name for this section
        public string start;           //text which denotes the beginning of this section in the save file.
        public string end;             //text which denotes the end of this section in the save file.
        public bool hidden;
        public List<ConfigItem> items; //Items within records in this section of the file
    }

    class ConfigSettings
    {
        public ConfigSettings()
        {
            sections = new List<ConfigSection>();
        }
        public List<ConfigSection> sections;
    }

    class Config
    {
        public Config(byte[] config)
        {
            using (MemoryStream stream = new MemoryStream(config))
                using(StreamReader reader = new StreamReader(stream))
                    init(reader);
        }

        public Config(string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using(StreamReader reader = new StreamReader(stream))
                    init(reader);
        }

        void init(StreamReader reader)
        {
            errors = "";
            settings = new ConfigSettings();
            ConfigSection currentSection = null;
            string line;
            while((line = reader.ReadLine()) != null)
            {
                line = line.Split('#')[0];
                line = line.Trim();
                if(line == "" || line[0] == '#')
                    continue;
                string[] parts = line.Split(',');
                if(parts.Length < 4)
                {
                    errors += "Not enough commas on line: " + line + "\r\n";
                    continue;
                }
                foreach(string s in parts)
                    s.Trim();

                if(parts[0] == "section")
                {
                    if(parts.Length > 5)
                    {
                        errors += "Too many commas on section line: " + line + "\r\n";
                        continue;
                    }
                    if(currentSection != null)
                        settings.sections.Add(currentSection);
                    currentSection = new ConfigSection();
                    currentSection.sectionName = parts[1];
                    currentSection.start = parts[2];
                    currentSection.end = parts[3];
                    if(parts.Length == 5)
                    {
                        if(!bool.TryParse(parts[4], out currentSection.hidden))
                        {
                            errors += "Couldn't 'hidden' on line: " + line + "\r\n";
                            continue;
                        }
                    }
                }
                else
                    if(parts[0] == "item")
                    {
                        if(currentSection == null)
                        {
                            errors += "Item line without section: " + line + "\r\n";
                            continue;
                        }
                        ConfigItem item = new ConfigItem();
                        int next = 1;
                        item.friendlyName = parts[next++];
                        item.serializedName = parts[next++];
                        item.type = parseType(parts[next++]);
                        if(item.type == typeof(Config))
                        {
                            errors += "Couldn't type on line: " + line + "\r\n";
                            continue;
                        }
                        if(item.type == typeof(Nullable))
                        {
                            if(!int.TryParse(parts[next++], out item.bytes))
                            {
                                errors += "Couldn't bytes on line: " + line + "\r\n";
                                continue;
                            }
                        }
                        if(parts.Length > next)
                        {
                            if(!bool.TryParse(parts[next++], out item.editable))
                            {
                                errors += "Couldn't 'editable' on line: " + line + "\r\n";
                                continue;
                            }
                        }
                        if(parts.Length > next)
                        {
                            if(!bool.TryParse(parts[next++], out item.hidden))
                            {
                                errors += "Couldn't 'hidden' on line: " + line + "\r\n";
                                continue;
                            }
                        }
                        if(parts.Length > next)
                        {
                            errors += "Too many commas on section line: " + line + "\r\n";
                            continue;
                        }
                        currentSection.items.Add(item);
                    }
                    else
                    {
                        errors += "Couldn't interpret line: " + line + "\r\n";
                        continue;
                    }
                }

            if(currentSection != null)
                settings.sections.Add(currentSection);
            reader.Close();
        }

        private System.Type parseType(string s)
        {
            switch(s)
            {
                case "int":
                    return typeof(int);
                case "short":
                    return typeof(short);
                case "char":
                    return typeof(char);
                case "string":
                    return typeof(string);
                case "float":
                    return typeof(float);
                case "block":
                    return typeof(Nullable);
                default:
                    return typeof(Config);  //error
            }
        }

        public ConfigSettings settings;
        public string errors;
    }
}
