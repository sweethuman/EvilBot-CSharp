namespace EvilBot.DataStructures
{
    public interface IUser
    {
        string DisplayName { get; set; }
        int Id { get; set; }
        string UserId { get; }
        string Points { get; set; }
        string Minutes { get; set; }
        string Rank { get; set; }
    }
}