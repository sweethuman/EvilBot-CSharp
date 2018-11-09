namespace EvilBot.DataStructures.Interfaces
{
    public interface IRankItem
    {
        int Id { get; }
        string Name { get; }
        int RequiredPoints { get; }
    }
}