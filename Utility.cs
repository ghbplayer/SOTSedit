using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;

namespace SOTSEdit
{
    class HackLevel
    {
        public HackLevel(int playerID, List<int> resources, int ownerIndex_)
        {
            player = playerID;
            levels = resources;
            ownerIndex = ownerIndex_;
            resourceIndex = 0;
        }

        public void hack1Any(Data item, int index)
        {
            item.ints[index] = Math.Max(item.ints[index], levels[resourceIndex]);
        }

        public void hack1p(Data item, int index)
        {
            if(item.ints[ownerIndex] == player)
                item.ints[index] = Math.Max(item.ints[index], levels[resourceIndex]);
        }

        public int resourceIndex;
        int player;
        int ownerIndex;
        List<int> levels;
    }

    class Utility
    {
        public void analyzeStrings(byte[] data, string fileName, ConfigSettings settings)
        {
            if(data.Length == 0)
            {
                MessageBox.Show("No data loaded.", "Cannot analyze", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StringAnalyzer an = new StringAnalyzer();
            an.data = data;
            an.fileName = fileName;
            an.settings = settings;
            System.Threading.Thread newThread = new System.Threading.Thread(an.threadStart);
            newThread.Start();
        }

        public int setInfrastructure(Parse p, List<float> resources, int player)
        {
            float min = Math.Min(1, resources[0]);
            float max = Math.Min(1, resources[1]);
            float min2 = Math.Max(0, resources[0] - 1);
            float max2 = Math.Max(0, resources[1] - 1);

            HackInfo infra = p.findAtt("Planets", "Infra");
            int ibonIndex = p.findAtt("Planets", "ibon").attribute.index;
            int ownerIndex = p.findAtt("Planets", "OID").attribute.index;

            Parse.Hack1 hack = (planet, index) =>
            {
                if(planet.ints[ownerIndex] == -1 || planet.ints[ownerIndex] != player)
                    return; //if player == 0, that's just fine. Don't want to alter those worlds anyways

                if(planet.floats[index] < min)
                    planet.floats[index] = min;
                else if(planet.floats[index] > max)
                    planet.floats[index] = max;

                if(planet.floats[ibonIndex] < min2)
                    planet.floats[ibonIndex] = min2;
                else if(planet.floats[ibonIndex] > max2)
                    planet.floats[ibonIndex] = max2;
            };

            p.performSimpleHack(infra, hack);
            
            return infra.dataset.index;
        }

        public int setSize(Parse p, List<float> resources, int player)
        {
            int min = (int)resources[0];
            int max = (resources[1] > 100) ? 100 : (int)resources[1];

            HackInfo size = p.findAtt("Planets", "Size");
            int ownerIndex = p.findAtt("Planets", "OID").attribute.index;

            Parse.Hack1 hack1p = (planet, index) =>
            {
                if(planet.ints[ownerIndex] != player)
                    return;

                if(planet.ints[index] < min)
                    planet.ints[index] = min;
                else if(planet.ints[index] > max)
                    planet.ints[index] = max;
            };

            Parse.Hack1 hackAny = (planet, index) =>
            {
                if(planet.ints[index] < min)
                    planet.ints[index] = min;
                else if(planet.ints[index] > max)
                    planet.ints[index] = max;
            };

            Parse.Hack1 hack = (player > 0) ? hack1p : hackAny;

            p.performSimpleHack(size, hack);
            
            return size.dataset.index;
        }

        public int cureAddiction(Parse p, int player)
        {
            HackInfo addiction = p.findAtt("Planets", "nadct");
            int ownerIndex = p.findAtt("Planets", "OID").attribute.index;

            Parse.Hack1 hack1p = (planet, index) =>
            {
                if(planet.ints[ownerIndex] != player)
                    return;

                planet.ints[index] = 0;
            };

            Parse.Hack1 hackAny = (planet, index) =>
            {
                planet.ints[index] = 0;
            };

            Parse.Hack1 hack = (player > 0) ? hack1p : hackAny;

            p.performSimpleHack(addiction, hack);
            
            return addiction.dataset.index;
        }

        public int terraformDustballs(Parse p, int player)
        {
            float lowMin = 5000;
            float highMax = -5000;
            HackInfo suit = p.findAtt("Players", "IdealSuit");
            int tolerance = p.findAtt("Players", "SuitTol").attribute.index;
            int playerIndex = p.findAtt("Players", "PlyrIdx").attribute.index;

            if(player > 0)
            {
                foreach(Data data in suit.dataset.data)
                {
                    if(data.ints[playerIndex] != player)
                        continue;
                    lowMin  = Math.Min(lowMin, data.floats[suit.attribute.index] - data.floats[tolerance]);
                    highMax = Math.Max(lowMin, data.floats[suit.attribute.index] + data.floats[tolerance]);
                }
            }
            else
            {
                foreach(Data data in suit.dataset.data)
                {
                    lowMin  = Math.Min(lowMin, data.floats[suit.attribute.index] - data.floats[tolerance]);
                    highMax = Math.Max(lowMin, data.floats[suit.attribute.index] + data.floats[tolerance]);
                }
            }

            int ownerIndex = p.findAtt("Planets", "OID").attribute.index;

            Parse.Hack1 hack = (planet, index) =>
            {
                if(planet.ints[ownerIndex] > 0 )
                    return;
                if(planet.floats[index] < lowMin)
                    planet.floats[index] = lowMin;
                else if(planet.floats[index] > highMax)
                    planet.floats[index] = highMax;
                else
                    return;
            };

            HackInfo suitP = p.findAtt("Planets", "Suit");
            p.performSimpleHack(suitP, hack);

            return suitP.dataset.index;
        }

        public int terraform(Parse p, int player)
        {
            if(player < 0)
                return -1;

            List<float> ideal = new List<float>(16);

            {
                HackInfo players = p.findAtt("Players", "IdealSuit");
                foreach(Data data in players.dataset.data)
                    ideal.Add(data.floats[players.attribute.index]);
            }

            int ownerIndex = p.findAtt("Planets", "OID").attribute.index;

            Parse.Hack1 hack1p = (planet, index) =>
            {
                if(planet.ints[ownerIndex] == player)
                    planet.floats[index] = ideal[player-1];
            };

            Parse.Hack1 hackAny = (planet, index) =>
            {
                int ownerID = planet.ints[ownerIndex];
                if(ownerID > 0 && ownerID < ideal.Count)
                    planet.floats[index] = ideal[ownerID-1];
            };

            Parse.Hack1 hack = (player != 0) ? hack1p : hackAny;

            p.performSimpleHack(p.findAtt("Planets", "Suit"), hack);
            
            return p.findAtt("Planets", "Idx").dataset.index;
        }

        public int setPopulation(Parse p, List<float> resources, int player)
        {
            HackInfo infoCiv  = p.findAtt("Planets", "PopC");
            HackInfo infoImp  = p.findAtt("Planets", "Pop");
            HackInfo infoImp2 = p.findAtt("Planets", "pbon");
            int ownerIndex    = p.findAtt("Planets", "OID").attribute.index;
            int sizeIndex     = p.findAtt("Planets", "Size").attribute.index;
            int resourceIndex = 0;

            Parse.Hack1 hack1p = (planet, index) =>
            {
                if(planet.ints[ownerIndex] == player)
                    planet.ints[index] = Math.Max(planet.ints[index], (int)(100000000 * planet.ints[sizeIndex] * resources[resourceIndex]));
            };

            Parse.Hack1 hackAny = (planet, index) =>
            {
                if(planet.ints[ownerIndex] > 1)
                    planet.ints[index] = Math.Max(planet.ints[index], (int)(100000000 * planet.ints[sizeIndex] * resources[resourceIndex]));
            };

            Parse.Hack1 hack = (player != 0) ? hack1p : hackAny;

            p.performSimpleHack(p.findAtt("Planets", "PopC"), hack);    ++resourceIndex;
            p.performSimpleHack(p.findAtt("Planets", "Pop"), hack);     ++resourceIndex;
            p.performSimpleHack(p.findAtt("Planets", "pbon"), hack);
            
            return p.findAtt("Planets", "PopC").dataset.index;
        }

        public int setResources(Parse p, List<int> resources, int player)
        {
            int ownerIndex = p.findAtt("Planets", "OID").attribute.index;
            int resourceIndex = 0;

            Parse.Hack1 hack1p = (item, index) =>
            {
                if(item.ints[ownerIndex] == player)
                    item.ints[index] = Math.Max(item.ints[index], resources[resourceIndex]);
            };

            Parse.Hack1 hackAny = (item, index) =>
            {
                item.ints[index] = Math.Max(item.ints[index], resources[resourceIndex]);
            };

            Parse.Hack1 hack = (player != 0) ? hack1p : hackAny;
            
            p.performSimpleHack(p.findAtt("Planets", "Res"), hack);     ++resourceIndex;
            p.performSimpleHack(p.findAtt("Planets", "ARes2"), hack);   ++resourceIndex;
            p.performSimpleHack(p.findAtt("Planets", "MRes"), hack);

            return p.findAtt("Planets", "Res").dataset.index;
        }
    }
    
    class StringLocComparator : IComparer<StringLoc>
    {
        public int Compare(StringLoc x, StringLoc y)
        {
            return x.offset < y.offset ? -1 : 1;
        }
    }

    class StringAnalyzer
    {
        public void threadStart()
        {
            Parse parser = new Parse(settings);
            List<StringLoc> list = parser.findAllShortStrings(data);

            List<StringLoc> first           = new List<StringLoc>(list.Count / 640);
            Dictionary<string, int> lastDic = new Dictionary<string, int>(list.Count / 640);
            Dictionary<string, int> countDic   = new Dictionary<string, int>(list.Count / 640);

            foreach(StringLoc loc in list)
            {
                if(!lastDic.ContainsKey(loc.text))
                {
                    first.Add(loc);
                    lastDic.Add(loc.text, loc.offset);
                    countDic.Add(loc.text, 1);
                }
                else
                {
                    lastDic[loc.text]  = loc.offset;
                    countDic[loc.text]++;
                }
            }
            
            List<StringLoc> last           = new List<StringLoc>(first.Count);
            foreach(KeyValuePair<string, int> pair in lastDic)
                last.Add(new StringLoc(pair.Key, pair.Value));
            lastDic = null;
            last.Sort(new StringLocComparator());

            List<StringLoc> count           = new List<StringLoc>(first.Count);
            foreach(KeyValuePair<string, int> pair in countDic)
                count.Add(new StringLoc(pair.Key, pair.Value));
            count.Sort(new StringLocComparator());

            int firstIt = 0;
            int lastIt = 0;
            int firstEnd = first.Count;
            int lastEnd = last.Count;

            //write the ordering file
            using (FileStream outFile = File.Create(fileName + ".anO"))
            {
                while(firstIt != firstEnd && lastIt != lastEnd)
                {
                    if(first[firstIt].offset < last[lastIt].offset)
                        writeStringFirst(outFile, first[firstIt].text, countDic[first[firstIt].text], ref firstIt);
                    else
                        writeStringLast(outFile, last[lastIt].text, countDic[last[lastIt].text], ref lastIt);
                }

                while(lastIt != lastEnd)
                    writeStringLast(outFile, last[lastIt].text, countDic[last[lastIt].text], ref lastIt);
            }

            //write the count file
            using (FileStream outFile = File.Create(fileName + ".anC"))
            {
                foreach(StringLoc loc in count)
                    writeString(outFile, loc.text + ":" + loc.offset.ToString());
            }

            MessageBox.Show("String analysis complete. Saving output files as " + fileName + ".an?.\r\nUse these files to help understand the save game format, and improve the editor.", "String Analyzer", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void writeStringFirst(FileStream stream, string str, int count, ref int iterator)
        {
            ++iterator;
            if (count == 1)
                writeString(stream, str + "++++++++");
            else
                writeString(stream, str);
        }

        private void writeStringLast(FileStream stream, string str, int count, ref int iterator)
        {
            ++iterator;
            if(count == 1)
                return;
            writeString(stream, str + "--------");
        }

        private void writeString(FileStream stream, string str)
        {
            stream.Write(System.Text.Encoding.ASCII.GetBytes(str + "\r\n"), 0, str.Length+2);
        }

        public byte[] data;
        public string fileName;
        public ConfigSettings settings;
    }
}
