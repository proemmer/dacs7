// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dacs7.Metadata
{
    public static class Dacs7ClientMetadataExtensions
    {

        /// <summary>
        /// Read the meta data of a block from the PLC.
        /// </summary>
        /// <param name="blockType">Specify the block type to read. e.g. DB   <see cref="PlcBlockType"/></param>
        /// <param name="blocknumber">Specify the Number of the block</param>
        /// <returns><see cref="IPlcBlockInfo"/> where you have access tho the detailed meta data of the block.</returns>
        public static async Task<IPlcBlockInfo> ReadBlockInfoAsync(this Dacs7Client client, PlcBlockType type, int blocknumber)
        {
            var result = await client.ProtocolHandler.ReadBlockInfoAsync(type, blocknumber).ConfigureAwait(false);
            if (result != null)
            {
                return new PlcBlockInfo
                {
                    ADDLength = result.ADDLength,
                    Author = result.Author?.Trim('\0'),
                    BlockFlags = result.BlockFlags,
                    BlockLanguage = result.BlockLanguage,
                    BlockNumber = result.BlockNumber,
                    BlockSecurity = result.BlockSecurity,
                    Checksum = result.Checksum,
                    CodeSize = result.CodeSize,
                    Family = result.Family?.Trim('\0'),
                    LastCodeChange = result.LastCodeChange,
                    LastInterfaceChange = result.LastInterfaceChange,
                    LengthLoadMemory = result.LengthLoadMemory,
                    LocalDataSize = result.LocalDataSize,
                    Name = result.Name?.Trim('\0'),
                    SSBLength = result.SSBLength,
                    SubBlockType = result.SubBlockType,
                    VersionHeaderMajor = result.VersionHeaderMajor,
                    VersionHeaderMinor = result.VersionHeaderMinor
                };
            }
            return null;
        }


        /// <summary>
        /// Reads the counts of all blocks available in the plc
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task<IPlcBlocksCount> ReadBlocksCountAsync(this Dacs7Client client)
        {
            var result = await client.ProtocolHandler.ReadBocksCountInfoAsync().ConfigureAwait(false);
            if (result != null)
            {
                return result.Counts;
            }
            return null;
        }

        public static Task<IEnumerable<IPlcBlock>> ReadBlocksOfTypeAsync(this Dacs7Client client, PlcBlockType type)
            => client.ProtocolHandler.ReadBlocksOfTypesAsync(type);

    }
}
