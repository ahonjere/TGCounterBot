using System;
using System.Collections.Generic;
using System.Text;

namespace CloudTgBotCore3
{
    public static partial class TelegramEndpoint
    {
        public static string GetLeaderboard(Users users)
        {
            List<KeyValuePair<string, int>> incs = new List<KeyValuePair<string, int>>();


            foreach (var pair in users.Accounts)
            {
                if (pair.Value != users.Accounts["all"])
                {
                    incs.Add(new KeyValuePair<string, int>(pair.Value.Name, pair.Value.Incs));
                }
            }

            incs.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            string msg_to_send = "";
            int i = 1;
            foreach (var pair in incs)
            {
                msg_to_send += i + ". " + pair.Key + ": " + pair.Value + Environment.NewLine;
                ++i;
            }
            return msg_to_send;


        }
        public static int GetIncrement(string[] msg)
        {
            int inc;
            int sign;
            if (msg[0] == "/inc1")
            {
                sign = 1;
            }
            else
            {
                sign = -1;
            }
            if (msg.Length == 1)
            {
                inc = sign;
            }
            else
            {
                int.TryParse(msg[1], out inc);
                inc *= sign;
            }
            return inc;
        }

    }
}
