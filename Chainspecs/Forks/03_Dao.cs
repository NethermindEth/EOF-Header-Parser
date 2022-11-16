namespace Nethermind.Specs
{
    public class Dao : Homestead
    {
        private static IReleaseSpec _instance;

        protected Dao()
        {
            Name = "DAO";
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new Dao());
    }
}
