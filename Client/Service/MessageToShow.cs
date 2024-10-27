namespace Client.Service
{
    public class MessageToShow(string content)
    {
        public string Content { get; private set; } = content;
    }
}
