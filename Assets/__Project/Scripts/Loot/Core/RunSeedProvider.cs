using System;

namespace Loot.Core
{
    public class RunSeedProvider : IRunSeedProvider
    {
        private int _seed;
        private bool _isSet;

        public int RunSeed
        {
            get
            {
                if (!_isSet)
                {
                    throw new InvalidOperationException(
                        "Run seed was read before area generation set it. " +
                        "Ensure SetSeed is called before any loot roll.");
                }

                return _seed;
            }
        }

        public void SetSeed(int seed)
        {
            _seed = seed;
            _isSet = true;
        }
    }
}
