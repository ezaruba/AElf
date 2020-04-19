using System.Linq;
using AElf.Kernel;
using AElf.TestBase;
using AElf.Types;

namespace AElf.CrossChain
{
    public class CrossChainTestBase : AElfIntegratedTest<CrossChainTestModule>
    {
        protected Address MinerAddress { get; } = SampleAddress.AddressList.First();
    }
}