namespace Nethermind.Specs
{
    public class GrayGlacier : ArrowGlacier
    {
        private static IReleaseSpec _instance;

        protected GrayGlacier()
        {
            Name = "Gray Glacier";
            DifficultyBombDelay = 11400000L;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new GrayGlacier());


    }
}
