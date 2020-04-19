using System.Threading.Tasks;
using AElf.CrossChain.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.CrossChain.Application
{
    public interface ICrossChainService
    {
        Task FinishInitialSyncAsync();
        Task UpdateCrossChainDataWithLibAsync(Hash blockHash, long blockHeight);

        Task<ByteString> GenerateExtraDataAsync(Hash blockHash, long blockHeight);

        Task<CrossChainTransactionInput> GetCrossChainTransactionInputForNextMiningAsync(Hash blockHash,
            long blockHeight, Address from);

        Task<bool> CheckExtraDataIsNeededAsync(Hash blockHash, long blockHeight, Timestamp timestamp);
    }
}