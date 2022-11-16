namespace Nethermind.Specs
{
    public class Istanbul : ConstantinopleFix
    {
        private static IReleaseSpec _instance;

        protected Istanbul()
        {
            Name = "Istanbul";
            IsEip1344Enabled = true;
            IsEip2028Enabled = true;
            IsEip152Enabled = true;
            IsEip1108Enabled = true;
            IsEip1884Enabled = true;
            IsEip2200Enabled = true;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new Istanbul());
    }
}
