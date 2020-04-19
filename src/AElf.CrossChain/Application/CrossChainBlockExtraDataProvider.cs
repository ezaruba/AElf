using System.Threading.Tasks;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Txn.Application;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Application
{
    internal class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainService _crossChainService;
        private readonly TransactionPackingOptions _transactionPackingOptions;

        public CrossChainBlockExtraDataProvider(IOptionsMonitor<TransactionPackingOptions> transactionPackingOptions, 
            ICrossChainService crossChainService)
        {
            _crossChainService = crossChainService;
            _transactionPackingOptions = transactionPackingOptions.CurrentValue;
        }

        public async Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == AElfConstants.GenesisBlockHeight)
                return ByteString.Empty;

            if (!_transactionPackingOptions.IsTransactionPackable)
                return ByteString.Empty;

            var bytes = await _crossChainService.GenerateExtraDataAsync(
                blockHeader.PreviousBlockHash, blockHeader.Height - 1);

            return bytes;
        }

        public string ExtraDataName => CrossChainConstants.CrossChainExtraDataNamePrefix;
    }
}