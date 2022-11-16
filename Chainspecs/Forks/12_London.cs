namespace Nethermind.Specs
{
    public class London : Berlin
    {
        private static IReleaseSpec _instance;

        protected London()
        {
            Name = "London";
            DifficultyBombDelay = 9700000L;
            IsEip1559Enabled = true;
            IsEip3198Enabled = true;
            IsEip3529Enabled = true;
            IsEip3541Enabled = true;
            Eip1559TransitionBlock = 12965000;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new London());
    }
}
