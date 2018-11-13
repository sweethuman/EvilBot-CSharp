namespace EvilBot.DataStructures.Interfaces
{
    public interface IUserStructure
    {
        string DisplayName { get; set; }
        int Id { get; set; }
        string UserId { get; }
        string Points { get; set; }
        string Minutes { get; set; }
        string Rank { get; set; }
    }
}