namespace Nethermind.Specs
{
    public class Homestead : Frontier
    {
        private static IReleaseSpec _instance;

        protected Homestead()
        {
            Name = "Homestead";
            IsEip2Enabled = true;
            IsEip7Enabled = true;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new Homestead());
    }
}
