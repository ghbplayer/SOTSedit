using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//TODO:
// Spiff up UI
// add shortcut keys
// Fix playerSettings strings if possible


namespace SOTSEdit
{
    class Attribute : ConfigItem
    {
        public Attribute(ConfigItem item, int index_)
        {
            type = item.type;
            serializedName = item.serializedName;
            friendlyName = item.friendlyName;
            editable = item.editable;
            hidden = item.hidden;
            index = index_;
        }

        public int index;
    }

    class AttributeFactory
    {
        public AttributeFactory(ref List<Attribute> list_)
        {
            list = list_;
        }

        public void add(ConfigItem item)
        {
            ++index;
            int typeIndex = 0;
            if (item.type == typeof(float))
                typeIndex = indexFloat++;
            else if (item.type == typeof(int) || item.type == typeof(short) || item.type == typeof(char))
                typeIndex = indexInt++;
            else if (item.type == typeof(string))
                typeIndex = indexString++;
            else if (item.type == typeof(Nullable))
                typeIndex = indexBlock++;
            list.Add(new Attribute(item, typeIndex));
        }
        
        public int indexBlock = 0;
        public int indexFloat = 0;
        public int indexInt = 0;
        public int indexString = 0;
        public int index = 0;
        List<Attribute> list;
    }

    class Data
    {
        public Data(AttributeFactory factory)
        {
            floats  = new float[factory.indexFloat];
            strings = new string[factory.indexString];
            ints    = new int[factory.indexInt];

            floats_orig  = new float[factory.indexFloat];
            strings_orig = new string[factory.indexString];
            ints_orig    = new int[factory.indexInt];

            floatIndices  = new int[factory.indexFloat];
            stringIndices = new int[factory.indexString];
            intIndices    = new int[factory.indexInt];
        }

        public float[] floats {get;set;}
        public string[] strings {get;set;}
        public int[] ints {get;set;}
        public float[] floats_orig;
        public string[] strings_orig;
        public int[] ints_orig;
        public int[] floatIndices;
        public int[] stringIndices;
        public int[] intIndices;
    }

    class TabDataset
    {
        public TabDataset(string name_, string sectionBegin_, string sectionEnd_, bool hidden_)
        {
            name = name_;
            sectionBegin = sectionBegin_;
            sectionEnd = sectionEnd_;
            recordMarker = "";
            hidden = hidden_;
            index = -1;
            data = new List<Data>();
            attributes = new List<Attribute>();
            factory = new AttributeFactory(ref attributes);
        }

        public List<Data> data;
        public AttributeFactory factory;
        public List<Attribute> attributes;
        public string name;
        public string sectionBegin;
        public string sectionEnd;
        public string recordMarker;
        public int index;
        public bool hidden;
    }

    class StringUpdate
    {
        public StringUpdate(ref string old_, ref string new_, int offset_)
        {
            oldS = old_;
            newS = new_;
            offset = offset_;
        }

        public int offset;
        public string oldS;
        public string newS;
    }

    class StringUpdateComparator : IComparer<StringUpdate>
    {
        public int Compare(StringUpdate x, StringUpdate y)
        {
            return x.offset > y.offset ? -1 : 1;
        }
    }

    class StringLoc
    {
        public StringLoc(string t, int o)
        {
            text = t;
            offset = o;
        }

        public string text;
        public int offset;
    }

    class HackInfo
    {
        public HackInfo(TabDataset ds, Attribute att)
        {
            dataset = ds;
            attribute = att;
        }
        public TabDataset dataset;
        public Attribute attribute;
    }

    class Parse
    {
        private byte[] data;
        private Dictionary<string, int> raceIDs;
        private Dictionary<int, string> raceNames;
        public List<TabDataset> datasets;

        public Parse(ConfigSettings settings)
        {
            datasets = new List<TabDataset>(settings.sections.Count);
            foreach(ConfigSection section in settings.sections)
            {
                TabDataset dataset = new TabDataset(section.sectionName, section.start, section.end, section.hidden);
                if(section.items.Count != 0)
                {
                    dataset.recordMarker = section.items[0].serializedName;
                    foreach(ConfigItem item in section.items)
                        dataset.factory.add(item);
                }

                datasets.Add(dataset);
            }

            raceIDs   = new Dictionary<string, int>();
            raceNames = new Dictionary<int, string>();

            addRace("Human", 0);
            addRace("Hiver", 1);
            addRace("Tarkas", 2);
            addRace("Liir", 3);
            addRace("_NPC", 4);
            addRace("Zuul", 5);
            addRace("Morrigi", 6);
        }

        public bool parseSOTS(byte[] binaryData)
        {
            clear();
            data = binaryData;
            
            int rangeStart = 0;
            int rangeEnd   = 0;

            for(int i = 0; i < datasets.Count; ++i)
            {
                TabDataset table = datasets[i];
                if(table.factory.index == 0)
                    continue;
                rangeStart = findBStr(table.sectionBegin, 0);
                if(table.sectionEnd != "")
                    rangeEnd   = findBStr(table.sectionEnd, rangeStart);
                else
                    rangeEnd    = data.Length;
                parseDataRegion(table.recordMarker, i, rangeStart, rangeEnd);
            }

            hack_postParse();
            data = null;
            return true;
        }

        public void updateRawData(ref byte[] binaryData)
        {
            data = binaryData;
            hack_preWrite();

            foreach(TabDataset dataset in datasets)
                foreach(Attribute att in dataset.attributes)
                {
                    if(!att.editable)
                        continue;
                    if (att.type == typeof(float))
                        writeFloats(dataset.data, att.index);
                    else if (att.type == typeof(int))
                        writeInts(dataset.data, att.index);
                    else if (att.type == typeof(short))
                        writeShorts(dataset.data, att.index);
                    else if (att.type == typeof(char))
                        writeChars(dataset.data, att.index);
                    //not writing blocks
                }
            //strings last... really.
            writeStrings();
            hack_postParse();   //for whatever reason, the WPF DataGrid picks up there changes and I just cannot refresh it, even though it's deleted and recreated with new data.

            binaryData = data;
            data = null;
        }

        public List<StringLoc> findAllShortStrings(byte[] rawData)
        {
            data = rawData;
            List<StringLoc> list = new List<StringLoc>();
            for(int i = 0; i < data.Length-5; ++i)
            {
                int len = BitConverter.ToInt32(data, i);
                if(len > 2 && len < 8)
                {
                    bool failed = false;
                    string str = System.Text.Encoding.ASCII.GetString(data, i + 4, len);
                    foreach(Char c in str)
                    {
                        if(!Char.IsLetterOrDigit(c))
                        {
                            failed = true;
                            break;
                        }
                    }
                    if(failed)
                        continue;

                    list.Add(new StringLoc(str, i));
                    i += 4 + len - 1;
                }
            }
            data = null;
            return list;
        }

        private void writeStrings()
        {
            List<StringUpdate> updates = new List<StringUpdate>(2000);
            int netDifference = 0;

            foreach(TabDataset dataset in datasets)
                for(int i = 0, maxI = dataset.data.Count; i < maxI; ++i)
                    for(int j = 0; j < dataset.factory.indexString; ++j)
                    {
                        Data row = dataset.data[i];
                        if(row.strings[j] != row.strings_orig[j])
                        {
                            //hack - adjusting string length doesn't always work... though it sometimes does. Fucked.
                            if(row.strings[j].Length > row.strings_orig[j].Length)
                                row.strings[j] = row.strings[j].Substring(0, row.strings_orig[j].Length);
                            else if(row.strings[j].Length < row.strings_orig[j].Length)
                                row.strings[j] += "                    ".Substring(0, row.strings_orig[j].Length - row.strings[j].Length);
                            netDifference += row.strings[j].Length - row.strings_orig[j].Length;
                            updates.Add(new StringUpdate(ref row.strings_orig[j], ref row.strings[j], row.stringIndices[j]));
                        }
                    }
            if(updates.Count == 0)
                return;
            StringUpdateComparator comparator = new StringUpdateComparator();
            updates.Sort(comparator.Compare);

            byte[] output = new byte[data.Length + netDifference];

            int nextReadEnd  = data.Length;
            int nextWriteEnd = output.Length;

            //update all strings
            foreach(StringUpdate update in updates)
            {
                //first, write the data block
                int nextReadBegin = update.offset + 4 + update.oldS.Length;
                int nextReadLength = nextReadEnd - nextReadBegin;
                writeBlock(data, ref output, nextReadBegin, ref nextWriteEnd, nextReadLength);
                nextReadEnd = update.offset;

                //next, write the new string
                byte[] bstr = makeBStrPattern(update.newS);
                nextReadBegin -= bstr.Length;
                writeBlock(bstr, ref output, 0, ref nextWriteEnd, bstr.Length);
            }

            //finally, write anything before the first string
            writeBlock(data, ref output, 0, ref nextWriteEnd, nextReadEnd);

            data = output;
        }

        private void writeBlock(byte[] input, ref byte[] output, int readBegin, ref int writeEnd, int readLength)
        {
            Buffer.BlockCopy(input, readBegin, output, writeEnd - readLength, readLength);
            writeEnd -= readLength;
        }

        private void writeFloats(List<Data> table, int index)
        {
            for(int i = 0, max = table.Count; i < max; ++i)
                if(table[i].floats[index] != table[i].floats_orig[index])
                {
                    Data row = table[i];
                    if(row.floatIndices[index] == -1)
                        continue;
                    if(row.floats_orig[index] != BitConverter.ToSingle(data, row.floatIndices[index]))
                        throw new Exception("Error: I've almost made a mistake when writing a float. Aborting.");
                    row.floats_orig[index] = row.floats[index];
                    byte[] temp = BitConverter.GetBytes(row.floats[index]);
                    Buffer.BlockCopy(temp, 0, data, row.floatIndices[index], temp.Length);
                    if(row.floats_orig[index] != BitConverter.ToSingle(data, row.floatIndices[index]))
                        throw new Exception("Error: I've made a mistake when writing a float. Aborting.");
                }
        }

        private void writeInts(List<Data> table, int index)
        {
            for(int i = 0, max = table.Count; i < max; ++i)
                if(table[i].ints[index] != table[i].ints_orig[index])
                {
                    Data row = table[i];
                    if(row.intIndices[index] == -1)
                        continue;
                    if(row.ints_orig[index] != BitConverter.ToInt32(data, row.intIndices[index]))
                        throw new Exception("Error: I've almost made a mistake when writing a int. Aborting.\r\nIf you see -1, it's probably not present in the file.");
                    row.ints_orig[index] = row.ints[index];
                    byte[] temp = BitConverter.GetBytes(row.ints[index]);
                    Buffer.BlockCopy(temp, 0, data, row.intIndices[index], temp.Length);
                    if(row.ints_orig[index] != BitConverter.ToInt32(data, row.intIndices[index]))
                        throw new Exception("Error: I've made a mistake when writing a int. Aborting.");
                }
        }

        private void writeShorts(List<Data> table, int index)
        {
            for(int i = 0, max = table.Count; i < max; ++i)
                if(table[i].ints[index] != table[i].ints_orig[index])
                {
                    Data row = table[i];
                    if(row.intIndices[index] == -1)
                        continue;
                    if(row.ints_orig[index] != BitConverter.ToInt16(data, row.intIndices[index]))
                        throw new Exception("Error: I've almost made a mistake when writing a short. Aborting.");
                    row.ints_orig[index] = row.ints[index];
                    byte[] temp = BitConverter.GetBytes((short)row.ints[index]);
                    Buffer.BlockCopy(temp, 0, data, row.intIndices[index], temp.Length);
                    if(row.ints_orig[index] != BitConverter.ToInt16(data, row.intIndices[index]))
                        throw new Exception("Error: I've made a mistake when writing a short. Aborting.");
                }
        }

        private void writeChars(List<Data> table, int index)
        {
            for(int i = 0, max = table.Count; i < max; ++i)
                if(table[i].ints[index] != table[i].ints_orig[index])
                {
                    Data row = table[i];
                    if(row.intIndices[index] == -1)
                        continue;
                    if(row.ints_orig[index] != BitConverter.ToChar(data, row.intIndices[index]))
                        throw new Exception("Error: I've almost made a mistake when writing a char. Aborting.");
                    row.ints_orig[index] = row.ints[index];
                    byte[] temp = BitConverter.GetBytes((char)row.ints[index]);
                    Buffer.BlockCopy(temp, 0, data, row.intIndices[index], temp.Length);
                    if(row.ints_orig[index] != BitConverter.ToChar(data, row.intIndices[index]))
                        throw new Exception("Error: I've made a mistake when writing a char. Aborting.");
                }
        }

        public HackInfo findAtt(string sectionName, string serializedName)
        {
            foreach(TabDataset set in datasets)
            {
                if(set.name != sectionName)
                    continue;
                foreach(Attribute att in set.attributes)
                {
                    if(att.serializedName != serializedName)
                        continue;
                    return new HackInfo(set, att);
                }
            }
            return new HackInfo(null, null);
        }

        public HackInfo findAttFr(string sectionName, string friendlyName)
        {
            foreach(TabDataset set in datasets)
            {
                if(set.name != sectionName)
                    continue;
                foreach(Attribute att in set.attributes)
                {
                    if(att.friendlyName != friendlyName)
                        continue;
                    return new HackInfo(set, att);
                }
            }
            return new HackInfo(null, null);
        }

        public delegate void Hack1(Data item, int attributeIndex);
        public delegate void Hack2(Data item, int attributeIndex, int attributeIndex2);

        private void hack_postParse()
        {
            performSimpleHack(findAttFr("Players", "SpeciesRAW"), findAttFr("Players", "Species"), (player, indexRAW, indexFriendly) => {player.strings_orig[indexFriendly] = player.strings[indexFriendly] = raceNames[player.ints[indexRAW]];});
            performSimpleHack(findAtt("Planets", "OID"), findAtt("Planets", "PID"), (planet, indexO, indexP) => {if(planet.ints[indexO] != -1){planet.ints[indexP] = planet.ints[indexO] /= 16;}});
            performSimpleHack(findAtt("Players", "PlyrIdx"), (player, index) => {player.ints[index] += 1;});
        }

        private void hack_preWrite()
        {
            //performSimpleHack(findAttFr("Players", "SpeciesRAW"), findAttFr("Players", "Species"), (player, indexRAW, indexFriendly) => {});

            Hack2 hackPID = (planet, indexO, indexP) => 
            {
                if(planet.ints[indexO] == -1)
                    return;
                planet.ints[indexP] = planet.ints[indexO] *= 16;
                if(planet.ints[indexO] == planet.ints_orig[indexO])
                    planet.ints[indexP] = planet.ints_orig[indexP];
            };

            performSimpleHack(findAtt("Planets", "OID"), findAtt("Planets", "PID"), hackPID);
            performSimpleHack(findAtt("Players", "PlyrIdx"), (player, index) => {player.ints[index] -= 1;});
            performSimpleHack(findAtt("PlayerSettings", "IsPlay"), findAtt("PlayerSettings", "IsDead"), (player, indexP, indexD) => {player.ints[indexP] = player.ints[indexD];});
        }
        
        public void performSimpleHack(HackInfo info, Hack1 hack)
        {
            if(info.attribute == null)
                return;
            foreach (Data item in info.dataset.data)
                hack(item, info.attribute.index);
        }

        public void performSimpleHack(HackInfo info, HackInfo info2, Hack2 hack)
        {
            if(info.attribute == null || info2.attribute == null)
                return;
            foreach (Data item in info.dataset.data)
                hack(item, info.attribute.index, info2.attribute.index);
        }

        private void parseDataRegion(string recordStartMarker, int datasetIndex, int rangeStart, int rangeEnd)
        {
            TabDataset dataset = datasets[datasetIndex];
            while(true)
            {
                rangeStart = findBStr(recordStartMarker, rangeStart, rangeEnd);
                if(rangeStart == -1)
                    break;
                int tooFar = findBStr(recordStartMarker, rangeStart + 1, rangeEnd);
                if (tooFar == -1)
                    tooFar = rangeEnd;
                parseSingleRecord(dataset.attributes, dataset.factory, dataset.data, ref rangeStart, tooFar);
            }
        }

        private void parseSingleRecord(List<Attribute> attributes, AttributeFactory factory, List<Data> list, ref int rangeStart, int rangeEnd)
        {
            Data data = new Data(factory);

            for (int j = 0; j < factory.index; ++j)
            {
                Attribute att = attributes[j];
                if(att.serializedName == "")    //hack
                    continue;
                if (att.type == typeof(float))
                    data.floats_orig[att.index]   = data.floats[att.index]  = findAndParseSingle(att.serializedName, ref data.floatIndices[att.index],  rangeStart, rangeEnd);
                else if (att.type == typeof(int))
                    data.ints_orig[att.index]     = data.ints[att.index]    = findAndParseInt(att.serializedName,    ref data.intIndices[att.index],    rangeStart, rangeEnd);
                else if (att.type == typeof(short))
                    data.ints_orig[att.index]     = data.ints[att.index]    = findAndParseShort(att.serializedName,  ref data.intIndices[att.index],    rangeStart, rangeEnd);
                else if (att.type == typeof(char))
                    data.ints_orig[att.index]     = data.ints[att.index]    = findAndParseChar(att.serializedName,   ref data.intIndices[att.index],    rangeStart, rangeEnd);
                else if (att.type == typeof(string))
                    data.strings_orig[att.index]  = data.strings[att.index] = findAndParseBStr(att.serializedName,   ref data.stringIndices[att.index], rangeStart, rangeEnd);
                else if (att.type == typeof(Nullable)) {}   //not reading raw blocks
            }
            list.Add(data);
            rangeStart = rangeEnd;
        }

        private int findFirstOf(byte[] pattern, int startOffset, int endOffset)
        {
            int i = Array.IndexOf<byte>(data, pattern[0], startOffset, endOffset-startOffset);
            while (i >= 0 && i <= endOffset - pattern.Length)
            {
                byte[] segment = new byte[pattern.Length];
                Buffer.BlockCopy(data, i, segment, 0, pattern.Length);
                if (segment.SequenceEqual<byte>(pattern))
                    return i;
                i = Array.IndexOf<byte>(data, pattern[0], i + 1, endOffset-i-1);
            }
            return -1;
        }

        private int findFirstOf(byte[] pattern, int offset)
        {
            return findFirstOf(pattern, offset, data.Length);
        }
        
        private int findBStr(string text, int startOffset, int endOffset)
        {
            byte[] pattern = makeBStrPattern(text);
            return findFirstOf(pattern, startOffset, endOffset);
        }
        
        private int findBStr(string text, int startOffset)
        {
            return findBStr(text, startOffset, data.Length);
        }

        private string findAndParseBStr(string text, ref int found, int startOffset, int endOffset)
        {
            found = findBStr(text, startOffset, endOffset);
            if(found == -1)
                return "###";
            found +=  4 + text.Length;
            return parseBStr(found);
        }

        private int findAndParseInt(string text, ref int found, int startOffset, int endOffset)
        {
            found = findBStr(text, startOffset, endOffset);
            if(found == -1)
                return -1;
            found +=  4 + text.Length;
            return BitConverter.ToInt32(data, found);
        }

        private int findAndParseShort(string text, ref int found, int startOffset, int endOffset)
        {
            found = findBStr(text, startOffset, endOffset);
            if(found == -1)
                return -1;
            found +=  4 + text.Length;
            return BitConverter.ToInt16(data, found);
        }

        private int findAndParseChar(string text, ref int found, int startOffset, int endOffset)
        {
            found = findBStr(text, startOffset, endOffset);
            if(found == -1)
                return -1;
            found +=  4 + text.Length;
            return BitConverter.ToChar(data, found);
        }

        private Single findAndParseSingle(string text, ref int found, int startOffset, int endOffset)
        {
            found = findBStr(text, startOffset, endOffset);
            if(found == -1)
                return Single.NaN;
            found +=  4 + text.Length;
            return BitConverter.ToSingle(data, found);
        }
        
        private string parseBStr(int offset)
        {
            int length = BitConverter.ToInt32(data, offset);
            return System.Text.Encoding.ASCII.GetString(data, offset + 4, length);
        }

        private byte[] makeBStrPattern(string input)
        {
            byte[] ret = new byte[input.Length + 4];
            Buffer.BlockCopy(BitConverter.GetBytes(input.Length), 0, ret, 0, 4);
            copyStringToBuffer(input, ref ret, 4);
            return ret;
        }
        
        private void copyStringToBuffer(string input, ref byte[] output, int outputOffset)
        {
            Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes(input), 0, output, outputOffset, input.Length);
        }

        private void addRace(string name, int id)
        {
            raceIDs.Add(name, id);
            raceNames.Add(id, name);
        }

        public void clear()
        {
            data = null;
            foreach(TabDataset table in datasets)
                table.data.Clear();
        }
    }
}
