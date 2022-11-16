namespace Nethermind.Specs
{
    public class TangerineWhistle : Dao
    {
        private static IReleaseSpec _instance;

        protected TangerineWhistle()
        {
            Name = "Tangerine Whistle";
            IsEip150Enabled = true;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new TangerineWhistle());
    }
}
