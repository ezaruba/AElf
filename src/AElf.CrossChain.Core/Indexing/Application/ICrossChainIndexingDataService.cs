using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.CrossChain.Indexing.Application
{
    public interface ICrossChainIndexingDataService
    {
        Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash blockHash, long blockHeight);

        Task<IndexedSideChainBlockData> GetIndexedSideChainBlockDataAsync(Hash blockHash, long blockHeight);

        Task<List<SideChainBlockData>> GetNonIndexedSideChainBlockDataAsync(Hash blockHash, long blockHeight);

        Task<List<ParentChainBlockData>> GetNonIndexedParentChainBlockDataAsync(Hash blockHash, long blockHeight);

        Task<SideChainIdAndHeightDict> GetAllChainIdHeightPairsAtLibAsync();
        Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId);
        Task<Block> GetNonIndexedBlockAsync(long requestHeight);

        Task<PendingCrossChainIndexingProposalDto> GetPendingCrossChainIndexingProposalAsync(Hash blockHash,
            long blockHeight, Timestamp timestamp, Address from = null);
    }

    public class PendingCrossChainIndexingProposalDto
    {
        public Hash ProposalId { get; set; }
        public Address Proposer { get; set; }
        public bool ToBeReleased { get; set; }
        public CrossChainBlockData ProposedCrossChainBlockData { get; set; }
        public Timestamp ExpiredTime { get; set; }
    }
}