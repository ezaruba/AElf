using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Indexing.Application;
using AElf.CrossChain.Infrastructure;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Application
{
    internal class CrossChainService : ICrossChainService
    {
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly ICrossChainIndexingProposalProvider _crossChainIndexingProposalProvider;

        public ILogger<CrossChainService> Logger { get; set; }

        public CrossChainService(ICrossChainCacheEntityService crossChainCacheEntityService,
            ICrossChainIndexingDataService crossChainIndexingDataService,
            ICrossChainIndexingProposalProvider crossChainIndexingProposalProvider)
        {
            _crossChainCacheEntityService = crossChainCacheEntityService;
            _crossChainIndexingDataService = crossChainIndexingDataService;
            _crossChainIndexingProposalProvider = crossChainIndexingProposalProvider;
        }

        public IOptionsMonitor<CrossChainConfigOptions> CrossChainConfigOptions { get; set; }

        public async Task FinishInitialSyncAsync()
        {
            CrossChainConfigOptions.CurrentValue.CrossChainDataValidationIgnored = false;
            var chainIdHeightPairs =
                await _crossChainIndexingDataService.GetAllChainIdHeightPairsAtLibAsync();
            foreach (var chainIdHeight in chainIdHeightPairs.IdHeightDict)
            {
                // register new chain
                _crossChainCacheEntityService.RegisterNewChain(chainIdHeight.Key, chainIdHeight.Value);
            }
        }

        public async Task UpdateCrossChainDataWithLibAsync(Hash blockHash, long blockHeight)
        {
            if (CrossChainConfigOptions.CurrentValue.CrossChainDataValidationIgnored
                || blockHeight <= AElfConstants.GenesisBlockHeight)
                return;

            _crossChainIndexingProposalProvider.ClearExpiredTransactionInput(blockHeight);

            var chainIdHeightPairs =
                await _crossChainIndexingDataService.GetAllChainIdHeightPairsAtLibAsync();

            await _crossChainCacheEntityService.UpdateCrossChainCacheAsync(blockHash, blockHeight, chainIdHeightPairs);
        }

        /// <summary>
        /// Generate cross chain extra data based on provided block.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public async Task<ByteString> GenerateExtraDataAsync(Hash blockHash, long blockHeight)
        {
            var utcNow = TimestampHelper.GetUtcNow();
            var pendingProposal =
                await _crossChainIndexingDataService.GetPendingCrossChainIndexingProposalAsync(blockHash, blockHeight,
                    utcNow);


            var crossChainIndexingPendingProposal = new CrossChainIndexingPendingProposal()
            {
                PreviousBlockHeight = blockHeight
            };

            if (pendingProposal == null)
            {
                _crossChainIndexingProposalProvider.AddCrossChainIndexingPendingProposal(blockHash,
                    crossChainIndexingPendingProposal);
                return ByteString.Empty;
            }

            var proposalDto = new CrossChainIndexingPendingProposalDto
            {
                ExpiredTime = pendingProposal.ExpiredTime,
                Proposer = pendingProposal.Proposer,
                ProposalId = pendingProposal.ProposalId,
                ToBeReleased = pendingProposal.ToBeReleased,
                IsExpired = pendingProposal.ExpiredTime.AddMilliseconds(500) <= utcNow
            };
            crossChainIndexingPendingProposal.PendingProposalInfo = proposalDto;
            _crossChainIndexingProposalProvider.AddCrossChainIndexingPendingProposal(blockHash,
                crossChainIndexingPendingProposal);

            if (proposalDto.IsExpired || !pendingProposal.ToBeReleased)
                return ByteString.Empty;

            // release pending proposal and unable to propose anything if it is ready
            Logger.LogInformation("Cross chain extra data generated.");
            return pendingProposal.ProposedCrossChainBlockData.ExtractCrossChainExtraDataFromCrossChainBlockData();
        }

        /// <summary>
        /// This method returns serialization input for cross chain proposing method.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="from"></param>
        /// <returns></returns>
        public async Task<CrossChainTransactionInput> GetCrossChainTransactionInputForNextMiningAsync(Hash blockHash,
            long blockHeight, Address from)
        {
            CrossChainTransactionInput inputForNextMining = null;
            var proposal = _crossChainIndexingProposalProvider.GetCrossChainIndexingPendingProposal(blockHash)
                .PendingProposalInfo;

            if (proposal == null || proposal.IsExpired)
            {
                // propose new cross chain indexing data if pending proposal is null or expired 
                var crossChainBlockData = await GetCrossChainBlockDataForNextMining(blockHash, blockHeight);
                if (!crossChainBlockData.IsNullOrEmpty())
                {
                    inputForNextMining = new CrossChainTransactionInput
                    {
                        MethodName =
                            nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing),
                        Value = crossChainBlockData.ToByteString()
                    };
                }
            }
            else if (proposal.ToBeReleased)
            {
                inputForNextMining = new CrossChainTransactionInput
                {
                    MethodName =
                        nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing),
                    Value = proposal.ProposalId.ToByteString()
                };
            }

            return inputForNextMining;
        }

        public async Task<bool> CheckExtraDataIsNeededAsync(Hash blockHash, long blockHeight, Timestamp timestamp)
        {
            var pendingProposal =
                await _crossChainIndexingDataService.GetPendingCrossChainIndexingProposalAsync(blockHash, blockHeight,
                    timestamp);
            return pendingProposal != null && pendingProposal.ToBeReleased && pendingProposal.ExpiredTime > timestamp;
        }

        private async Task<CrossChainBlockData> GetCrossChainBlockDataForNextMining(Hash blockHash,
            long blockHeight)
        {
            var sideChainBlockData =
                await _crossChainIndexingDataService.GetNonIndexedSideChainBlockDataAsync(blockHash, blockHeight);
            var parentChainBlockData =
                await _crossChainIndexingDataService.GetNonIndexedParentChainBlockDataAsync(blockHash, blockHeight);

            var crossChainBlockData = new CrossChainBlockData
            {
                PreviousBlockHeight = blockHeight,
                ParentChainBlockDataList = {parentChainBlockData},
                SideChainBlockDataList = {sideChainBlockData}
            };
            return crossChainBlockData;
        }
    }
}