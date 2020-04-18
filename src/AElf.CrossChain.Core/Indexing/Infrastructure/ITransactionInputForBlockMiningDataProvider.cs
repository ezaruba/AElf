using AElf.Contracts.CrossChain;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.CrossChain.Indexing.Infrastructure
{
    public interface ITransactionInputForBlockMiningDataProvider
    {
        void AddTransactionInputForBlockMining(Hash blockHash, CrossChainTransactionInput crossChainTransactionInput);

        CrossChainTransactionInput GetTransactionInputForBlockMining(Hash blockHash);

        void ClearExpiredTransactionInput(long blockHeight);
    }

    public class CrossChainTransactionInput
    {
        public long PreviousBlockHeight { get; set; }
        public string MethodName { get; set; }
        public ByteString Value { get; set; }

        public CrossChainIndexingPendingProposalDto PendingProposalInfo { get; set; }
    }

    public class CrossChainIndexingPendingProposalDto
    {
        public Hash ProposalId { get; set; }
        public Address Proposer { get; set; }
        public bool ToBeReleased { get; set; }
        public Timestamp ExpiredTime { get; set; }
        
        public bool IsExpired { get; set; }
    }
}