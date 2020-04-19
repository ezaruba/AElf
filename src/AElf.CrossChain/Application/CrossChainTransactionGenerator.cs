using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Application
{
    internal class CrossChainTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ICrossChainService _crossChainService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILogger<CrossChainTransactionGenerator> Logger { get; set; }

        public CrossChainTransactionGenerator(
            ISmartContractAddressService smartContractAddressService, ICrossChainService crossChainService)
        {
            _smartContractAddressService = smartContractAddressService;
            _crossChainService = crossChainService;
        }

        private async Task<List<Transaction>> GenerateCrossChainIndexingTransactionAsync(Address from,
            long refBlockNumber,
            Hash previousBlockHash)
        {
            var generatedTransactions = new List<Transaction>();

            var crossChainTransactionInput =
                await _crossChainService.GetCrossChainTransactionInputForNextMiningAsync(previousBlockHash,
                    refBlockNumber, from);

            if (crossChainTransactionInput == null)
            {
                return generatedTransactions;
            }

            var previousBlockPrefix = BlockHelper.GetRefBlockPrefix(previousBlockHash).ToByteArray();
            generatedTransactions.Add(GenerateNotSignedTransaction(from, crossChainTransactionInput.MethodName,
                refBlockNumber, previousBlockPrefix, crossChainTransactionInput.Value));

            Logger.LogTrace($"Cross chain transaction generated.");
            return generatedTransactions;
        }

        public async Task<List<Transaction>> GenerateTransactionsAsync(Address @from, long preBlockHeight,
            Hash preBlockHash)
        {
            var generatedTransactions =
                await GenerateCrossChainIndexingTransactionAsync(from, preBlockHeight, preBlockHash);
            return generatedTransactions;
        }

        /// <summary>
        /// Create a txn with provided data.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="methodName"></param>
        /// <param name="refBlockNumber"></param>
        /// <param name="refBlockPrefix"></param> 
        /// <param name="bytes"></param>
        /// <returns></returns>
        private Transaction GenerateNotSignedTransaction(Address from, string methodName, long refBlockNumber,
            byte[] refBlockPrefix, ByteString bytes)
        {
            return new Transaction
            {
                From = from,
                To = _smartContractAddressService.GetAddressByContractName(
                    CrossChainSmartContractAddressNameProvider.Name),
                RefBlockNumber = refBlockNumber,
                RefBlockPrefix = ByteString.CopyFrom(refBlockPrefix),
                MethodName = methodName,
                Params = bytes,
            };
        }
    }
}