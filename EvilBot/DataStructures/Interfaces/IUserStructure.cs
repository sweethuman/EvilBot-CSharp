namespace EvilBot.DataStructures.Interfaces
{
    public interface IUserStructure : IUserBase
    {
        int Id { get; set; }        
        string Points { get; set; }
        string Minutes { get; set; }
        string Rank { get; set; }
    }
}
