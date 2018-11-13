using Dacs7.Metadata;
using System.Threading.Tasks;

namespace Dacs7
{
    public partial class Dacs7Client
    {

        /// <summary>
        /// Read the meta data of a block from the PLC.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB   <see cref="PlcBlockType"/></param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <returns><see cref="IPlcBlockInfo"/> where you have access tho the detailed meta data of the block.</returns>
        public async Task<IPlcBlockInfo> ReadBlockInfoAsync(PlcBlockType type, int blocknumber)
        {
            var result =  await _protocolHandler.ReadBlockInfoAsync(type, blocknumber);
            if(result != null)
            {
                return new PlcBlockInfo
                {
                    ADDLength = result.ADDLength,
                    Author = result.Author,
                    BlockFlags = result.BlockFlags,
                    BlockLanguage = result.BlockLanguage,
                    BlockNumber = result.BlockNumber,
                    BlockSecurity = result.BlockSecurity,
                    Checksum = result.Checksum,
                    CodeSize = result.CodeSize,
                    Family = result.Family,
                    LastCodeChange = result.LastCodeChange,
                    LastInterfaceChange = result.LastInterfaceChange,
                    LengthLoadMemory = result.LengthLoadMemory,
                    LocalDataSize = result.LocalDataSize,
                    Name = result.Name,
                    SSBLength = result.SSBLength,
                    SubBlockType = result.SubBlockType,
                    VersionHeaderMajor = result.VersionHeaderMajor,
                    VersionHeaderMinor = result.VersionHeaderMinor
                };
            }
            return null;
        }

    }
}
