namespace Nethermind.Specs
{
    public class Frontier : Olympic
    {
        private static IReleaseSpec _instance;

        protected Frontier()
        {
            Name = "Frontier";
            IsTimeAdjustmentPostOlympic = true;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new Frontier());

    }
}
