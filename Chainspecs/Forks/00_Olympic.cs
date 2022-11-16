namespace Nethermind.Specs
{
    public class Olympic : ReleaseSpec
    {
        private static IReleaseSpec _instance;

        protected Olympic()
        {
            Name = "Olympic";
            MaximumExtraDataSize = 32;
            MaxCodeSize = long.MaxValue;
            MinGasLimit = 5000;
            GasLimitBoundDivisor = 0x0400;
            BlockReward = 5000000000000000000L;
            DifficultyBoundDivisor = 0x0800;
            IsEip3607Enabled = true;
            MaximumUncleCount = 2;
            Eip1559TransitionBlock = long.MaxValue;
            ValidateChainId = true;
            ValidateReceipts = true;
        }

        public static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new Olympic());
    }
}
