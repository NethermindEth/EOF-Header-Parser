namespace Nethermind.Specs
{
    public class ConstantinopleFix : Constantinople
    {
        private static IReleaseSpec _instance;

        protected ConstantinopleFix()
        {
            Name = "Constantinople Fix";
            IsEip1283Enabled = false;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new ConstantinopleFix());
    }
}
