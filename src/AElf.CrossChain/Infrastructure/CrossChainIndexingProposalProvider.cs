using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Infrastructure
{
    internal class CrossChainIndexingProposalProvider : ICrossChainIndexingProposalProvider, ISingletonDependency
    {
        private readonly Dictionary<Hash, CrossChainIndexingPendingProposal> _indexedCrossChainBlockData =
            new Dictionary<Hash, CrossChainIndexingPendingProposal>();

        public void AddCrossChainIndexingPendingProposal(Hash blockHash, CrossChainIndexingPendingProposal crossChainIndexingProposal)
        {
            _indexedCrossChainBlockData[blockHash] = crossChainIndexingProposal;
        }

        public CrossChainIndexingPendingProposal GetCrossChainIndexingPendingProposal(Hash blockHash)
        {
            return _indexedCrossChainBlockData.TryGetValue(blockHash, out var crossChainBlockData)
                ? crossChainBlockData
                : null;
        }

        public void ClearExpiredTransactionInput(long blockHeight)
        {
            var toRemoveList = _indexedCrossChainBlockData.Where(kv => kv.Value.PreviousBlockHeight < blockHeight)
                .Select(kv => kv.Key);
            foreach (var hash in toRemoveList)
            {
                _indexedCrossChainBlockData.Remove(hash);
            }
        }
    }
}