namespace Hub.View
{
    /// <summary>
    /// The proximity presenter's handle on one spot's F prompt (O1 rework): show it on the
    /// nearest in-range spot, hide it everywhere else.
    /// </summary>
    public interface IHubPromptView
    {
        void ShowPrompt(bool visible);
    }
}
