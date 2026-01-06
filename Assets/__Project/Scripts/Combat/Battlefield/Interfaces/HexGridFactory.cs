using Zenject;

namespace Combat.Battlefield
{
    public interface IHexGridFactory
    {
        IHexGrid Create(HexOrientation orientation);
    }
    
    public class HexGridFactory : IHexGridFactory
    {
        private readonly DiContainer container;
        
        public HexGridFactory(DiContainer container)
        {
            this.container = container;
        }
        
        public IHexGrid Create(HexOrientation orientation)
        {
            return orientation == HexOrientation.Flat
                ? container.Instantiate<FlatHexGrid>()
                : container.Instantiate<PointyHexGrid>();
        }
    }
}

