using System.Collections.Generic;
using System.Linq;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Indexing.Application;

namespace AElf.CrossChain
{
    public class CrossChainTestHelper
    {
        private readonly Dictionary<long, CrossChainBlockData> _fakeIndexedCrossChainBlockData =
            new Dictionary<long, CrossChainBlockData>();

        private readonly List<SideChainBlockData> _sideChainBlockDataList = new List<SideChainBlockData>();
        private readonly List<ParentChainBlockData> _parentChainBlockDataList = new List<ParentChainBlockData>();

        private readonly Dictionary<int, long> _chainIdHeight = new Dictionary<int, long>();
        private GetPendingCrossChainIndexingProposalOutput _pendingCrossChainIndexingProposalOutput;

        public void AddFakeIndexedCrossChainBlockData(long height, CrossChainBlockData crossChainBlockData)
        {
            _fakeIndexedCrossChainBlockData.Add(height, crossChainBlockData);
        }

        public CrossChainBlockData GetIndexedCrossChainExtraData(long height)
        {
            return _fakeIndexedCrossChainBlockData.TryGetValue(height, out var crossChainBlockData)
                ? crossChainBlockData
                : null;
        }

        public void AddFakeChainIdHeight(int chainId, long libHeight)
        {
            _chainIdHeight.Add(chainId, libHeight);
        }

        public SideChainIdAndHeightDict GetAllIndexedCrossChainExtraData()
        {
            var sideChainIdAndHeightDict = new SideChainIdAndHeightDict
            {
                IdHeightDict = {_chainIdHeight}
            };
            return sideChainIdAndHeightDict;
        }

        internal void AddFakePendingCrossChainIndexingProposal(
            GetPendingCrossChainIndexingProposalOutput pendingCrossChainIndexingProposalOutput)
        {
            _pendingCrossChainIndexingProposalOutput = pendingCrossChainIndexingProposalOutput;
        }

        internal PendingCrossChainIndexingProposalDto GetFakePendingCrossChainIndexingProposal()
        {
            return _pendingCrossChainIndexingProposalOutput == null
                ? null
                : new PendingCrossChainIndexingProposalDto
                {
                    ProposalId = _pendingCrossChainIndexingProposalOutput.ProposalId,
                    ExpiredTime = _pendingCrossChainIndexingProposalOutput.ExpiredTime,
                    Proposer = _pendingCrossChainIndexingProposalOutput.Proposer,
                    ProposedCrossChainBlockData = _pendingCrossChainIndexingProposalOutput.ProposedCrossChainBlockData,
                    ToBeReleased = _pendingCrossChainIndexingProposalOutput.ToBeReleased
                };
        }

        internal void AddSideChainBlockDataList(List<SideChainBlockData> sideChainBlockDataList)
        {
            _sideChainBlockDataList.AddRange(sideChainBlockDataList);
        }
        
        internal List<SideChainBlockData> GetSideChainBlockDataList()
        {
            return _sideChainBlockDataList.ToList();
        }
        
        internal void AddParentChainBlockDataList(List<ParentChainBlockData> parentChainBlockDataList)
        {
            _parentChainBlockDataList.AddRange(parentChainBlockDataList);
        }
        
        internal List<ParentChainBlockData> GetParentChainBlockDataList()
        {
            return _parentChainBlockDataList.ToList();
        }
    }
}