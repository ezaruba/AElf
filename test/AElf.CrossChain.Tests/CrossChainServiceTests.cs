using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Application;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Infrastructure;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainServiceTest : CrossChainTestBase
    {
        private readonly ICrossChainService _crossChainService;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
        private readonly ICrossChainIndexingProposalProvider _crossChainIndexingProposalProvider;

        public CrossChainServiceTest()
        {
            _crossChainService = GetRequiredService<ICrossChainService>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
            _crossChainCacheEntityService = GetRequiredService<ICrossChainCacheEntityService>();
            _crossChainIndexingProposalProvider = GetRequiredService<ICrossChainIndexingProposalProvider>();
        }

        [Fact]
        public async Task FinishInitialSync_Test()
        {
            int chainId = ChainHelper.ConvertBase58ToChainId("AELF");
            long libHeight = 10;
            _crossChainTestHelper.AddFakeChainIdHeight(chainId, libHeight);
            await _crossChainService.FinishInitialSyncAsync();

            var height = _crossChainCacheEntityService.GetTargetHeightForChainCacheEntity(chainId);
            Assert.Equal(libHeight + 1, height);
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_NoProposal_FirstTimeIndexing_Test()
        {
            var res = await _crossChainService.GenerateExtraDataAsync(Hash.Empty, 1);
            Assert.Empty(res);
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_NoProposal_Test()
        {
            var preHash = HashHelper.ComputeFromString("Genesis");
            var res = await _crossChainService.GenerateExtraDataAsync(preHash, 1);
            res.ShouldBeEmpty();
            var proposal = _crossChainIndexingProposalProvider.GetCrossChainIndexingPendingProposal(preHash);
            proposal.PendingProposalInfo.ShouldBeNull();
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_NotApproved_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<SideChainBlockData>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFromString((sideChainId + 1).ToString())
                });
            }

            var pendingCrossChainIndexingProposalOutput = new GetPendingCrossChainIndexingProposalOutput
            {
                Proposer = SampleAddress.AddressList[0],
                ProposalId = HashHelper.ComputeFromString("ProposalId"),
                ProposedCrossChainBlockData = new CrossChainBlockData
                {
                    SideChainBlockDataList = {sideChainBlockInfoCache}
                },
                ToBeReleased = false,
                ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(pendingCrossChainIndexingProposalOutput);
            var preHash = HashHelper.ComputeFromString("Genesis");

            var res = await _crossChainService.GenerateExtraDataAsync(preHash, 1);
            Assert.Empty(res);

            var proposal = _crossChainIndexingProposalProvider.GetCrossChainIndexingPendingProposal(preHash);
            proposal.PendingProposalInfo.ProposalId.ShouldBe(pendingCrossChainIndexingProposalOutput.ProposalId);
        }


        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<SideChainBlockData>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFromString((sideChainId + 1).ToString())
                });
            }

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
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

            var preHash = HashHelper.ComputeFromString("Genesis");
            var res = await _crossChainService.GenerateExtraDataAsync(preHash, 1);
            var crossChainExtraData = CrossChainExtraData.Parser.ParseFrom(res);
            var expectedMerkleTreeRoot = BinaryMerkleTree
                .FromLeafNodes(sideChainBlockInfoCache.Select(s => s.TransactionStatusMerkleTreeRoot)).Root;
            expectedMerkleTreeRoot.ShouldBe(crossChainExtraData.TransactionStatusMerkleTreeRoot);
            res.ShouldBe(crossChainBlockData.ExtractCrossChainExtraDataFromCrossChainBlockData());

            var proposal = _crossChainIndexingProposalProvider.GetCrossChainIndexingPendingProposal(preHash);
            proposal.PendingProposalInfo.ProposalId.ShouldBe(pendingCrossChainIndexingProposalOutput.ProposalId);
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_EmptyExtraData_Test()
        {
            int parentChainId = 123;
            var parentChainBlockDataList = new List<ParentChainBlockData>();

            for (int i = 0; i < CrossChainConstants.DefaultBlockCacheEntityCount + 1; i++)
            {
                parentChainBlockDataList.Add(new ParentChainBlockData
                    {
                        Height = i + 1,
                        ChainId = parentChainId
                    }
                );
            }

            var pendingCrossChainIndexingProposalOutput = new GetPendingCrossChainIndexingProposalOutput
            {
                Proposer = SampleAddress.AddressList[0],
                ProposalId = HashHelper.ComputeFromString("ProposalId"),
                ProposedCrossChainBlockData = new CrossChainBlockData
                {
                    ParentChainBlockDataList = {parentChainBlockDataList}
                },
                ToBeReleased = true,
                ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(pendingCrossChainIndexingProposalOutput);


            var preHash = HashHelper.ComputeFromString("Genesis");
            var res = await _crossChainService.GenerateExtraDataAsync(preHash, 1);
            res.ShouldBeEmpty();

            var proposal = _crossChainIndexingProposalProvider.GetCrossChainIndexingPendingProposal(preHash);
            proposal.PendingProposalInfo.ProposalId.ShouldBe(pendingCrossChainIndexingProposalOutput.ProposalId);
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_Expired_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<SideChainBlockData>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFromString((sideChainId + 1).ToString())
                });
            }

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
            };

            var pendingCrossChainIndexingProposalOutput = new GetPendingCrossChainIndexingProposalOutput
            {
                Proposer = SampleAddress.AddressList[0],
                ProposalId = HashHelper.ComputeFromString("ProposalId"),
                ProposedCrossChainBlockData = crossChainBlockData,
                ToBeReleased = false,
                ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(-1)
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(pendingCrossChainIndexingProposalOutput);

            var preHash = HashHelper.ComputeFromString("Genesis");
            var res = await _crossChainService.GenerateExtraDataAsync(preHash, 1);
            res.ShouldBeEmpty();
            var proposal = _crossChainIndexingProposalProvider.GetCrossChainIndexingPendingProposal(preHash);
            proposal.PendingProposalInfo.ProposalId.ShouldBe(pendingCrossChainIndexingProposalOutput.ProposalId);
        }


        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_AlmostExpired_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<SideChainBlockData>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFromString((sideChainId + 1).ToString())
                });
            }

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
            };

            var pendingCrossChainIndexingProposalOutput = new GetPendingCrossChainIndexingProposalOutput
            {
                Proposer = SampleAddress.AddressList[0],
                ProposalId = HashHelper.ComputeFromString("ProposalId"),
                ProposedCrossChainBlockData = crossChainBlockData,
                ToBeReleased = true,
                ExpiredTime = TimestampHelper.GetUtcNow().AddMilliseconds(500)
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(pendingCrossChainIndexingProposalOutput);

            var preHash = HashHelper.ComputeFromString("Genesis");
            var res = await _crossChainService.GenerateExtraDataAsync(preHash, 1);
            var crossChainExtraData = CrossChainExtraData.Parser.ParseFrom(res);
            var expectedMerkleTreeRoot = BinaryMerkleTree
                .FromLeafNodes(sideChainBlockInfoCache.Select(s => s.TransactionStatusMerkleTreeRoot)).Root;
            crossChainExtraData.TransactionStatusMerkleTreeRoot.ShouldBe(expectedMerkleTreeRoot);
            crossChainBlockData.ExtractCrossChainExtraDataFromCrossChainBlockData().ShouldBe(res);

            var proposal = _crossChainIndexingProposalProvider.GetCrossChainIndexingPendingProposal(preHash);
            proposal.PendingProposalInfo.ProposalId.ShouldBe(pendingCrossChainIndexingProposalOutput.ProposalId);
        }


        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_AlmostExpired_NotApproved_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<SideChainBlockData>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFromString((sideChainId + 1).ToString())
                });
            }

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
            };

            var pendingCrossChainIndexingProposalOutput = new GetPendingCrossChainIndexingProposalOutput
            {
                Proposer = SampleAddress.AddressList[0],
                ProposalId = HashHelper.ComputeFromString("ProposalId"),
                ProposedCrossChainBlockData = crossChainBlockData,
                ToBeReleased = false,
                ExpiredTime = TimestampHelper.GetUtcNow().AddMilliseconds(500)
            };

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(pendingCrossChainIndexingProposalOutput);

            var preHash = HashHelper.ComputeFromString("Genesis");
            var res = await _crossChainService.GenerateExtraDataAsync(preHash, 1);
            res.ShouldBeEmpty();

            var proposal = _crossChainIndexingProposalProvider.GetCrossChainIndexingPendingProposal(preHash);
            proposal.PendingProposalInfo.ProposalId.ShouldBe(pendingCrossChainIndexingProposalOutput.ProposalId);
        }

        [Fact]
        public async Task GetCrossChainBlockDataForNextMining_WithoutCachingParentBlock_Test()
        {
            var sideChainId = 123;
            var blockInfoCache = new List<SideChainBlockData>();
            for (int i = 0; i < CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFromString(i.ToString())
                });
            }

            _crossChainTestHelper.AddSideChainBlockDataList(blockInfoCache);

            var preHash = HashHelper.ComputeFromString("Genesis");
            var crossChainIndexingPendingProposal = new CrossChainIndexingPendingProposal
            {
                PreviousBlockHeight = 1
            };
            _crossChainIndexingProposalProvider.AddCrossChainIndexingPendingProposal(preHash,
                crossChainIndexingPendingProposal);
            var crossChainTransactionInput =
                await _crossChainService.GetCrossChainTransactionInputForNextMiningAsync(preHash, 1, MinerAddress);

            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(crossChainTransactionInput.Value);
            crossChainBlockData.SideChainBlockDataList.Count.ShouldBe(CrossChainConstants.DefaultBlockCacheEntityCount);
            crossChainBlockData.ParentChainBlockDataList.ShouldBeEmpty();
        }


        [Fact]
        public async Task GetCrossChainBlockDataForNextMining_WithoutCachingSideBlock_Test()
        {
            var parentChainId = 123;
            var blockInfoCache = new List<ParentChainBlockData>();
            for (int i = 0; i < CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData()
                {
                    ChainId = parentChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFromString(i.ToString())
                });
            }

            _crossChainTestHelper.AddParentChainBlockDataList(blockInfoCache);

            var preHash = HashHelper.ComputeFromString("Genesis");
            var crossChainIndexingPendingProposal = new CrossChainIndexingPendingProposal
            {
                PreviousBlockHeight = 1
            };
            _crossChainIndexingProposalProvider.AddCrossChainIndexingPendingProposal(preHash,
                crossChainIndexingPendingProposal);
            var crossChainTransactionInput =
                await _crossChainService.GetCrossChainTransactionInputForNextMiningAsync(preHash, 1, MinerAddress);
            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(crossChainTransactionInput.Value);
            crossChainBlockData.ParentChainBlockDataList.Count.ShouldBe(
                CrossChainConstants.DefaultBlockCacheEntityCount);
            crossChainBlockData.SideChainBlockDataList.ShouldBeEmpty();
        }

        [Fact]
        public async Task GenerateTransactionInput_PendingProposal_Test()
        {
            var crossChainIndexingPendingProposal = new CrossChainIndexingPendingProposal
            {
                PendingProposalInfo = new CrossChainIndexingPendingProposalDto
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = HashHelper.ComputeFromString("ProposalId"),
                    IsExpired = false,
                    ToBeReleased = true,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
                }
            };
            var preHash = HashHelper.ComputeFromString("Genesis");
            _crossChainIndexingProposalProvider.AddCrossChainIndexingPendingProposal(preHash,
                crossChainIndexingPendingProposal);

            var crossChainTransactionInput =
                await _crossChainService.GetCrossChainTransactionInputForNextMiningAsync(preHash, 1, MinerAddress);

            crossChainTransactionInput.MethodName.ShouldBe(nameof(CrossChainContractContainer.CrossChainContractStub
                .ReleaseCrossChainIndexing));

            var proposalIdInParam = Hash.Parser.ParseFrom(crossChainTransactionInput.Value);
            proposalIdInParam.ShouldBe(crossChainIndexingPendingProposal.PendingProposalInfo.ProposalId);
        }

        [Fact]
        public async Task GenerateTransaction_PendingProposal_NotApproved_Test()
        {
            var crossChainIndexingPendingProposal = new CrossChainIndexingPendingProposal
            {
                PendingProposalInfo = new CrossChainIndexingPendingProposalDto
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = HashHelper.ComputeFromString("ProposalId"),
                    ToBeReleased = false,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10),
                    IsExpired = false
                }
            };

            var preHash = HashHelper.ComputeFromString("Genesis");
            _crossChainIndexingProposalProvider.AddCrossChainIndexingPendingProposal(preHash,
                crossChainIndexingPendingProposal);

            var crossChainTransactionInput =
                await _crossChainService.GetCrossChainTransactionInputForNextMiningAsync(preHash, 1, MinerAddress);
            crossChainTransactionInput.ShouldBeNull();
        }

        
        [Fact]
        public async Task FillExtraData_Test()
        {
            var fakeMerkleTreeRoot1 = HashHelper.ComputeFromString("fakeMerkleTreeRoot1");
            var fakeMerkleTreeRoot2 = HashHelper.ComputeFromString("fakeMerkleTreeRoot2");
            var fakeMerkleTreeRoot3 = HashHelper.ComputeFromString("fakeMerkleTreeRoot3");
        
            int chainId1 = ChainHelper.ConvertBase58ToChainId("2112");
            int chainId2 = ChainHelper.ConvertBase58ToChainId("2113");
            int chainId3 = ChainHelper.ConvertBase58ToChainId("2114");
            var fakeSideChainBlockDataList = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    Height = 1,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot1,
                    ChainId = chainId1
                },
                new SideChainBlockData
                {
                    Height = 1,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot2,
                    ChainId = chainId2
                },
                new SideChainBlockData
                {
                    Height = 1,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot3,
                    ChainId = chainId3
                }
            };
        
            var list1 = new List<SideChainBlockData>();
            var list2 = new List<SideChainBlockData>();
            var list3 = new List<SideChainBlockData>();
        
            list1.Add(fakeSideChainBlockDataList[0]);
            list2.Add(fakeSideChainBlockDataList[1]);
            list3.Add(fakeSideChainBlockDataList[2]);
        
            for (int i = 2; i < CrossChainConstants.DefaultBlockCacheEntityCount + 2; i++)
            {
                list1.Add(new SideChainBlockData
                {
                    Height = i,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot1,
                    ChainId = chainId1
                });
                list2.Add(new SideChainBlockData
                {
                    Height = i,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot2,
                    ChainId = chainId2
                });
                list3.Add(new SideChainBlockData
                {
                    Height = i,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot3,
                    ChainId = chainId3
                });
            }
        
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = HashHelper.ComputeFromString("ProposalId"),
                    ProposedCrossChainBlockData = new CrossChainBlockData
                    {
                        SideChainBlockDataList = {list1, list2, list3}
                    },
                    ToBeReleased = true,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
                });
        
            var header = new BlockHeader
            {
                PreviousBlockHash = HashHelper.ComputeFromString("PreviousHash"),
                Height = 2
            };
        
            var sideChainTxMerkleTreeRoot =
                await _crossChainService.GenerateExtraDataAsync(header.PreviousBlockHash,
                    header.Height - 1);
            var merkleTreeRoot = BinaryMerkleTree
                .FromLeafNodes(list1.Concat(list2).Concat(list3).Select(sideChainBlockData =>
                    sideChainBlockData.TransactionStatusMerkleTreeRoot)).Root;
            var expected = new CrossChainExtraData {TransactionStatusMerkleTreeRoot = merkleTreeRoot}.ToByteString();
            sideChainTxMerkleTreeRoot.ShouldBe(expected);
        }
    }
}