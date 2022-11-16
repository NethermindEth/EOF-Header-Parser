namespace Nethermind.Specs
{
    public class Shanghai : GrayGlacier
    {
        private static IReleaseSpec _instance;

        protected Shanghai()
        {
            Name = "Shanghai";
            IsEip1153Enabled = true;
            IsEip3675Enabled = true;
            IsEip3651Enabled = true;
            IsEip3855Enabled = true;
            IsEip3860Enabled = true;
            IsEip3670Enabled = true;
            IsEip3540Enabled = true;
            IsEip4200Enabled = true;
            IsEip4750Enabled = true;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new Shanghai());
    }
}
