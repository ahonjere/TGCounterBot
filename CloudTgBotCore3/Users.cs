using System.Collections.Generic;



namespace CloudTgBotCore3
{
    public static partial class TelegramEndpoint
    {
        public class Users
        {
            public Dictionary<string, User> Accounts = new Dictionary<string, User>();
        }
    }
}
