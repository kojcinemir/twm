namespace TilingWindowManager
{
    public class WindowSearchEntry
    {
        public nint WindowHandle { get; set; }
        public string Title { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string ExecutableName { get; set; } = "";
        public int WorkspaceId { get; set; }
        public int MonitorIndex { get; set; }
        public nint Icon { get; set; }
        public int FuzzyScore { get; set; }
    }
}
