namespace EvilBot.DataStructures.Database.Interfaces
{
    public interface IDatabaseUser
    {
        int Id { get; set; }
        string UserID { get; set; }
        string Points { get; set; }
        string Minutes { get; set; }
        string Rank { get; set; }
    }
}