namespace Dacs7
{

    public interface IPlcBlocks
    {
        int Number { get; }
        byte Flags { get; }
        string Language { get; }
    }
}
