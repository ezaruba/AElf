using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Infrastructure;
using AElf.Kernel;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.CrossChain
{
    public sealed class CrossChainIndexingTransactionGeneratorTest : CrossChainTestBase
    {
        private readonly ISystemTransactionGenerator _crossChainIndexingTransactionGenerator;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        private readonly ICrossChainIndexingProposalProvider _crossChainIndexingProposalProvider;


        public CrossChainIndexingTransactionGeneratorTest()
        {
            _crossChainIndexingTransactionGenerator = GetRequiredService<ISystemTransactionGenerator>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
            _crossChainIndexingProposalProvider = GetRequiredService<ICrossChainIndexingProposalProvider>();
        }

        [Fact]
        public async Task GenerateTransactions_Test()
        {
            var sideChainId = 123;
            var previousBlockHash = HashHelper.ComputeFromString("PreviousBlockHash");
            var previousBlockHeight = 1;
            var crossChainBlockData = new CrossChainBlockData
            {
                PreviousBlockHeight = previousBlockHeight
            };
            
            var cachingCount = 5;
            for (int i = 1; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                var sideChainBlockData = new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFromString((sideChainId + 1).ToString())
                };
                if (i <= CrossChainConstants.DefaultBlockCacheEntityCount)
                    crossChainBlockData.SideChainBlockDataList.Add(sideChainBlockData);
            }

            
            _crossChainTestHelper.AddSideChainBlockDataList(crossChainBlockData.SideChainBlockDataList.ToList());
            var crossChainIndexingPendingProposal = new CrossChainIndexingPendingProposal
            {
                PreviousBlockHeight = previousBlockHeight
            };

            _crossChainIndexingProposalProvider.AddCrossChainIndexingPendingProposal(previousBlockHash,
                crossChainIndexingPendingProposal);

            var smartContractAddress = SampleAddress.AddressList[0];
            _smartContractAddressService.SetAddress(CrossChainSmartContractAddressNameProvider.Name,
                smartContractAddress);

            var transactions =
                await _crossChainIndexingTransactionGenerator.GenerateTransactionsAsync(SampleAddress.AddressList[0],
                    previousBlockHeight, previousBlockHash);

            transactions.Count.ShouldBe(1);
            transactions[0].From.ShouldBe(SampleAddress.AddressList[0]);
            transactions[0].To.ShouldBe(smartContractAddress);
            transactions[0].RefBlockNumber.ShouldBe(previousBlockHeight);
            transactions[0].RefBlockPrefix.ShouldBe(ByteString.CopyFrom(previousBlockHash.Value.Take(4).ToArray()));
            transactions[0].MethodName
                .ShouldBe(nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing));

            var crossChainBlockDataInParam = CrossChainBlockData.Parser.ParseFrom(transactions[0].Params);
            Assert.Equal(crossChainBlockData, crossChainBlockDataInParam);
        }
        
        [Fact]
        public async Task GenerateTransaction_NoTransaction_Test()
        {
            var previousBlockHash = HashHelper.ComputeFromString("PreviousBlockHash");
            var previousBlockHeight = 1;

            var smartContractAddress = SampleAddress.AddressList[0];
            _smartContractAddressService.SetAddress(CrossChainSmartContractAddressNameProvider.Name,
                smartContractAddress);
        
            var crossChainIndexingPendingProposal = new CrossChainIndexingPendingProposal
            {
                PreviousBlockHeight = previousBlockHeight
            };

            _crossChainIndexingProposalProvider.AddCrossChainIndexingPendingProposal(previousBlockHash,
                crossChainIndexingPendingProposal);
            var transactions =
                await _crossChainIndexingTransactionGenerator.GenerateTransactionsAsync(SampleAddress.AddressList[0],
                    previousBlockHeight, previousBlockHash);
        
            transactions.ShouldBeEmpty();
        }
    }
}