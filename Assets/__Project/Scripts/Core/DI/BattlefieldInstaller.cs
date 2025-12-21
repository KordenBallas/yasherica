using Zenject;
using Battlefield;

namespace Core.DI
{
    public class BattlefieldInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Bind hex grid factories
            Container.BindFactory<FlatHexGrid, FlatHexGrid.Factory>();
            Container.BindFactory<PointyHexGrid, PointyHexGrid.Factory>();
            
            // Bind battlefield factory
            Container.BindFactory<IBattlefield, BattlefieldFactory>();
            
            // Bind hex grid factory (with orientation parameter support)
            Container.Bind<IHexGridFactory>().To<HexGridFactory>().AsSingle();
        }
    } 
}

