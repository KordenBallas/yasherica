namespace Hub.View
{
    /// <summary>
    /// Adapter contract for the cauldron's voice plaque on the Hub (O1): one line of smug
    /// gut-commentary at a time. An empty line hides the plaque.
    /// </summary>
    public interface IHubVoiceView
    {
        void ShowLine(string line);
    }
}
