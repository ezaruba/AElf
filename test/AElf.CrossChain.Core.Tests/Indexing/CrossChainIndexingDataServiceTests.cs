using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Indexing.Application;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Xunit;

namespace AElf.CrossChain.Indexing
{
    public class CrossChainIndexingDataServiceTests : CrossChainTestBase
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly CrossChainTestHelper _crossChainTestHelper;

        public CrossChainIndexingDataServiceTests()
        {
            _crossChainIndexingDataService = GetRequiredService<ICrossChainIndexingDataService>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }

        #region Side chain

        [Fact]
        public async Task GetIndexedCrossChainBlockData_WithIndex_Test()
        {
            var chainId = _chainOptions.ChainId;
            var fakeMerkleTreeRoot1 = HashHelper.ComputeFromString("fakeMerkleTreeRoot1");
            var fakeSideChainBlockData = new SideChainBlockData
            {
                Height = 1,
                ChainId = chainId,
                TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot1
            };

            var fakeIndexedCrossChainBlockData = new CrossChainBlockData();
            fakeIndexedCrossChainBlockData.SideChainBlockDataList.AddRange(new[] {fakeSideChainBlockData});

            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(fakeSideChainBlockData.Height,
                fakeIndexedCrossChainBlockData);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);

            AddFakeCacheData(new Dictionary<int, List<ICrossChainBlockEntity>>
            {
                {
                    chainId,
                    new List<ICrossChainBlockEntity>
                    {
                        fakeSideChainBlockData
                    }
                }
            });

            var res = await _crossChainIndexingDataService.GetIndexedCrossChainBlockDataAsync(
                fakeSideChainBlockData.BlockHeaderHash, 1);
            Assert.True(res.SideChainBlockDataList[0].Height == fakeSideChainBlockData.Height);
            Assert.True(res.SideChainBlockDataList[0].ChainId == chainId);
        }

        [Fact]
        public async Task GetIndexedCrossChainBlockData_WithoutIndex_Test()
        {
            var chainId = _chainOptions.ChainId;
            var fakeSideChainBlockData = new SideChainBlockData
            {
                Height = 1,
                ChainId = chainId
            };

            var fakeIndexedCrossChainBlockData = new CrossChainBlockData();
            fakeIndexedCrossChainBlockData.SideChainBlockDataList.AddRange(new[] {fakeSideChainBlockData});

            var res = await _crossChainIndexingDataService.GetIndexedCrossChainBlockDataAsync(
                fakeSideChainBlockData.BlockHeaderHash, 1);
            Assert.True(res == null);
        }

        #endregion
        
        
        [Fact]
        public async Task GetNonIndexedBlock_Test()
        {
            _crossChainTestHelper.SetFakeLibHeight(2);
            var res = await _crossChainIndexingDataService.GetNonIndexedBlockAsync(1);
            Assert.True(res.Height.Equals(1));
        }
        
        [Fact]
        public async Task GetNonIndexedBlock_NoBlock_Test()
        {
            _crossChainTestHelper.SetFakeLibHeight(1);
            var res = await _crossChainIndexingDataService.GetNonIndexedBlockAsync(2);
            Assert.Null(res);
        }
    }
}