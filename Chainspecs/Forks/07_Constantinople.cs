namespace Nethermind.Specs
{
    public class Constantinople : Byzantium
    {
        private static IReleaseSpec _instance;

        protected Constantinople()
        {
            Name = "Constantinople";
            BlockReward = 2000000000000000000;
            DifficultyBombDelay = 5000000L;
            IsEip145Enabled = true;
            IsEip1014Enabled = true;
            IsEip1052Enabled = true;
            IsEip1283Enabled = true;
            IsEip1234Enabled = true;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new Constantinople());
    }
}
