using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs5;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold
{
    internal class MethodCallingThresholdPreExecutionPlugin : SmartContractExecutionPluginBase, IPreExecutionPlugin, ISingletonDependency
    {
        private readonly IHostSmartContractBridgeContextService _contextService;
        private readonly IContractReaderFactory<ThresholdSettingContractContainer.ThresholdSettingContractStub>
            _thresholdSettingContractReaderFactory;
        private readonly IContractReaderFactory<TokenContractContainer.TokenContractStub>
            _tokenContractReaderFactory;

        public MethodCallingThresholdPreExecutionPlugin(IHostSmartContractBridgeContextService contextService,
            IContractReaderFactory<ThresholdSettingContractContainer.ThresholdSettingContractStub>
                thresholdSettingContractReaderFactory,
            IContractReaderFactory<TokenContractContainer.TokenContractStub> tokenContractReaderFactory) : base("acs5")
        {
            _contextService = contextService;
            _thresholdSettingContractReaderFactory = thresholdSettingContractReaderFactory;
            _tokenContractReaderFactory = tokenContractReaderFactory;
        }

        public async Task<IEnumerable<Transaction>> GetPreTransactionsAsync(
            IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
        {
            if (!IsTargetAcsSymbol(descriptors))
            {
                return new List<Transaction>();
            }

            var context = _contextService.Create();
            var thresholdSettingStub = _thresholdSettingContractReaderFactory.Create(new ContractReaderContext
            {
                BlockHash = transactionContext.PreviousBlockHash,
                BlockHeight = transactionContext.BlockHeight - 1,
                ContractAddress = transactionContext.Transaction.To,
                Sender = transactionContext.Transaction.To,
                Timestamp = transactionContext.CurrentBlockTime
            });

            var threshold = await thresholdSettingStub.GetMethodCallingThreshold.CallAsync(new StringValue
            {
                Value = transactionContext.Transaction.MethodName
            });

            // Generate token contract stub.
            var tokenContractAddress = context.GetContractAddressByName(TokenSmartContractAddressNameProvider.Name);
            if (tokenContractAddress == null)
            {
                return new List<Transaction>();
            }

            var tokenStub = _tokenContractReaderFactory.Create(new ContractReaderContext
            {
                Sender = transactionContext.Transaction.To,
                ContractAddress = tokenContractAddress
            });
            if (transactionContext.Transaction.To == tokenContractAddress &&
                transactionContext.Transaction.MethodName == nameof(tokenStub.CheckThreshold))
            {
                return new List<Transaction>();
            }

            var checkThresholdTransaction = tokenStub.CheckThreshold.GetTransaction(new CheckThresholdInput
            {
                Sender = transactionContext.Transaction.From,
                SymbolToThreshold = {threshold.SymbolToAmount},
                IsCheckAllowance = threshold.ThresholdCheckType == ThresholdCheckType.Allowance
            });

            return new List<Transaction>
            {
                checkThresholdTransaction
            };
        }

        public bool IsStopExecuting(ByteString txReturnValue)
        {
            return false;
        }
    }
}