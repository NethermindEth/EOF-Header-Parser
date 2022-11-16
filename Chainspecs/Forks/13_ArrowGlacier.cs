namespace Nethermind.Specs
{
    public class ArrowGlacier : London
    {
        private static IReleaseSpec _instance;

        protected ArrowGlacier()
        {
            Name = "ArrowGlacier";
            DifficultyBombDelay = 10700000L;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new ArrowGlacier());
    }
}
