using System;
using System.Collections.Generic;
using System.IO;


namespace UpvoteBot
{
    class DataUser
    {
        private ulong id;
        private string userName;
        private string fileName;

        private string UpvoteEmoteString = "<:upvote:807841816669716481>";
        private string UparrowEmoteString = "⬆";
        private string DownvoteEmoteString = "<:downvote:807841539287416842>";
        private string DownarrowEmoteString = "⬇";

        public ulong ID
        {
            get => id;
            set => id = value;
        }

        public string UserName
        {
            get => userName;
            set => userName = value;
        }

        private Dictionary<string, uint> data = new Dictionary<string, uint>();

        public DataUser(ulong IDin, string userNameIn)
        {
            id = IDin;
            userName = userNameIn;
            fileName = $"{id}.txt";
            if (!File.Exists(fileName))
            {
                FileStream temp = File.Create(fileName);
                temp.Close();
            }
            string[] array = File.ReadAllLines(fileName);
            Console.WriteLine($"Opening Data for {id}.");
            foreach (string fullitem in array)
            {
                string[] splititem = fullitem.Split(", ");
                data.Add(splititem[0], Convert.ToUInt32(splititem[1]));
                Console.WriteLine($"    {splititem[0]} : {splititem[1]}.");
            }
            Console.WriteLine("    Finish.");
        }

        public void save()
        {
            List<string> list = new List<string>();
            foreach(KeyValuePair<string, uint> kvp in data)
            {
                list.Add($"{kvp.Key}, {kvp.Value}");
            }
            File.WriteAllLines(fileName, list);
        }

        public void addEmote(string emoteIn)
        {
            if(data.ContainsKey(emoteIn))
            {
                data.TryGetValue(emoteIn, out uint k);
                k++;
                data[emoteIn] = k;
            } else
            {
                data.Add(emoteIn, 1);
            }
        }

        public void subtractEmote(string emoteIn)
        {
            if (data.ContainsKey(emoteIn))
            {
                data.TryGetValue(emoteIn, out uint k);
                if (k > 0)
                {
                    k--;
                    data[emoteIn] = k;
                } else
                {
                    k = 0;
                    data[emoteIn] = k;
                }
            }
            else
            {
                data.Add(emoteIn, 0);
            }
        }

        public bool tryGetCount(string emoteIn, out uint count)
        {
            if (data.ContainsKey(emoteIn))
            {
                return data.TryGetValue(emoteIn, out count);
            } else
            {
                count = 0;
                return false;
            }
        }

        public long getKarma()
        {
            uint countA;
            uint countB;
            uint countC;
            uint countD;
            tryGetCount(UpvoteEmoteString, out countA);
            tryGetCount(UparrowEmoteString, out countB);
            tryGetCount(DownvoteEmoteString, out countC);
            tryGetCount(DownarrowEmoteString, out countD);
            long count = countA + countB;
            count -= countC;
            count -= countD;
            return count;
        }
    }
}
