using System;


namespace CloudTgBotCore3
{
    public static partial class TelegramEndpoint
    {
        public class User
        {
            public string Name;
            public string UserName;
            public int Incs = 0;
            public DateTime FirstInc;
        }     
    }
}
