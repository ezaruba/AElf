using System.Threading.Tasks;
using AElf.CrossChain.Application;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(
        typeof(CrossChainAElfModule),
        typeof(KernelCoreTestAElfModule),
        typeof(SmartContractAElfModule)
    )]
    public class CrossChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<CrossChainTestHelper>();
            context.Services.AddTransient(provider =>
            {
                var mockCrossChainRequestService = new Mock<ICrossChainRequestService>();
                mockCrossChainRequestService.Setup(mock => mock.RequestCrossChainDataFromOtherChainsAsync())
                    .Returns(Task.CompletedTask);
                return mockCrossChainRequestService.Object;
            });
            
            context.Services.AddTransient(provider =>
            {
                var mockCrossChainIndexingDataService = new Mock<ICrossChainIndexingDataService>();
                mockCrossChainIndexingDataService
                    .Setup(m => m.GetIndexedCrossChainBlockDataAsync(It.IsAny<Hash>(), It.IsAny<long>()))
                    .Returns<Hash, long>((blockHash, blockHeight) =>
                    {
                        var crossChainTestHelper =
                            context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;

                        return Task.FromResult(crossChainTestHelper.GetIndexedCrossChainExtraData(blockHeight));
                    });

                mockCrossChainIndexingDataService.Setup(m =>
                    m.GetPendingCrossChainIndexingProposalAsync(It.IsAny<Hash>(), It.IsAny<long>(), It.IsAny<Timestamp>(), It.IsAny<Address>())).Returns(() =>
                {
                    var crossChainTestHelper =
                        context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                    return Task.FromResult(crossChainTestHelper.GetFakePendingCrossChainIndexingProposal());
                });
                
                mockCrossChainIndexingDataService.Setup(m =>
                    m.GetNonIndexedSideChainBlockDataAsync(It.IsAny<Hash>(), It.IsAny<long>())).Returns(() =>
                {
                    var crossChainTestHelper =
                        context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                    return Task.FromResult(crossChainTestHelper.GetSideChainBlockDataList());
                });
                
                mockCrossChainIndexingDataService.Setup(m =>
                    m.GetNonIndexedParentChainBlockDataAsync(It.IsAny<Hash>(), It.IsAny<long>())).Returns(() =>
                {
                    var crossChainTestHelper =
                        context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                    return Task.FromResult(crossChainTestHelper.GetParentChainBlockDataList());
                });
                
                mockCrossChainIndexingDataService.Setup(m =>
                    m.GetAllChainIdHeightPairsAtLibAsync()).Returns(() =>
                {
                    var crossChainTestHelper =
                        context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                    return Task.FromResult(crossChainTestHelper.GetAllIndexedCrossChainExtraData());
                });
                return mockCrossChainIndexingDataService.Object;
            });
        }
    }
}