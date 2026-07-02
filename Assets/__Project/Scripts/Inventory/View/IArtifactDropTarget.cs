namespace Inventory.View
{
    /// <summary>
    /// Anything on the 3D inventory stage a dragged artifact bubble can be
    /// released onto (blank sockets). Keeps the drag router agnostic of who
    /// consumes drops - the target forwards the artifact instance id upward.
    /// </summary>
    public interface IArtifactDropTarget
    {
        void NotifyArtifactDropped(int artifactInstanceId);
    }
}
