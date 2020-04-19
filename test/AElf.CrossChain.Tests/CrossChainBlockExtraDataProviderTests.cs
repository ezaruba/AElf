using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataProviderTest : CrossChainTestBase
    {
        private readonly IBlockExtraDataProvider _crossChainBlockExtraDataProvider;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        private readonly TransactionPackingOptions _transactionPackingOptions;

        public CrossChainBlockExtraDataProviderTest()
        {
            _crossChainBlockExtraDataProvider = GetRequiredService<IBlockExtraDataProvider>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
            _transactionPackingOptions = GetRequiredService<IOptionsMonitor<TransactionPackingOptions>>().CurrentValue;
        }

        [Fact]
        public async Task FillExtraData_GenesisHeight_Test()
        {
            var header = new BlockHeader
            {
                PreviousBlockHash = HashHelper.ComputeFromString("PreviousHash"),
                Height = 1
            };
            var bytes = await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            Assert.Empty(bytes);
        }

        [Fact]
        public async Task FIllExtraData_TransactionPackingDisabled()
        {
            var crossChainBlockData = PrepareCrossChainBlockData();
            var header = new BlockHeader
            {
                PreviousBlockHash = HashHelper.ComputeFromString("PreviousHash"),
                Height = 2
            };
            _transactionPackingOptions.IsTransactionPackable = false;
            var bytes = await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            bytes.ShouldBeEmpty();
        }

        [Fact]
        public async Task FillExtraData_Test()
        {
            var header = new BlockHeader
            {
                PreviousBlockHash = HashHelper.ComputeFromString("PreviousHash"),
                Height = 2
            };
            var crossChainBlockData = PrepareCrossChainBlockData();
            var bytes = await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            var crossChainExtraData = new CrossChainExtraData
            {
                TransactionStatusMerkleTreeRoot = BinaryMerkleTree
                    .FromLeafNodes(
                        crossChainBlockData.SideChainBlockDataList
                            .Select(
                                sideChainBlockData => sideChainBlockData.TransactionStatusMerkleTreeRoot)).Root
            };
            
            bytes.ShouldBe(crossChainExtraData.ToByteString());
        }

        private CrossChainBlockData PrepareCrossChainBlockData()
        {
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList =
                {
                    new SideChainBlockData()
                    {
                        ChainId = 123,
                        Height = 1,
                        TransactionStatusMerkleTreeRoot = HashHelper.ComputeFromInt32(1)
                    }
                }
            };
            var pendingCrossChainIndexingProposalOutput = new GetPendingCrossChainIndexingProposalOutput
            {
                Proposer = SampleAddress.AddressList[0],
                ProposalId = HashHelper.ComputeFromString("ProposalId"),
                ProposedCrossChainBlockData = crossChainBlockData,
                ToBeReleased = true,
                ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
            };
            
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(pendingCrossChainIndexingProposalOutput);
            return crossChainBlockData;
        }
    }
}