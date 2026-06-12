namespace Loot.Application
{
    public class UnlimitedCapacityPolicy : IInventoryCapacityPolicy
    {
        public bool CanAccept(string artifactId)
        {
            return true;
        }
    }
}
