namespace Nethermind.Specs
{
    public class MuirGlacier : Istanbul
    {
        private static IReleaseSpec _instance;

        protected MuirGlacier()
        {
            Name = "Muir Glacier";
            DifficultyBombDelay = 9000000L;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new MuirGlacier());
    }
}
