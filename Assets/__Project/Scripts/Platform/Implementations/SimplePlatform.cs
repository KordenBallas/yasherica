using Zenject;

namespace Platform
{
    public class SimplePlatform : Platform
    {
        public SimplePlatform(int id) : base(id)
        {
        }
        
        public class Factory : PlaceholderFactory<SimplePlatform>
        {
        }
    }
}
