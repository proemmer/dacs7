namespace Dacs7.Metadata
{

    public interface IPlcBlocks
    {
        int Number { get; }
        byte Flags { get; }
        string Language { get; }
    }
}
