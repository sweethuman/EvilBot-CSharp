namespace EvilBot.DataStructures
{
    public class UserBase : IUserBase
    {
        public UserBase(string name, string id)
        {
            DisplayName = name;
            UserId = id;
        }

        public string DisplayName { get; protected set; }
        public string UserId { get; protected set; }
    }
}