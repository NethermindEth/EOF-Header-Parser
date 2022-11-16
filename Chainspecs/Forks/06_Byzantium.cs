namespace Nethermind.Specs
{
    public class Byzantium : SpuriousDragon
    {
        private static IReleaseSpec _instance;

        protected Byzantium()
        {
            Name = "Byzantium";
            BlockReward = 3000000000000000000;
            DifficultyBombDelay = 3000000L;
            IsEip100Enabled = true;
            IsEip140Enabled = true;
            IsEip196Enabled = true;
            IsEip197Enabled = true;
            IsEip198Enabled = true;
            IsEip211Enabled = true;
            IsEip214Enabled = true;
            IsEip649Enabled = true;
            IsEip658Enabled = true;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new Byzantium());
    }
}
