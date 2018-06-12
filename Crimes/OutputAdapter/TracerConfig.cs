namespace OutputAdapter
{
    public enum TracerKind
    {
        Trace,

        Debug,

        Console,

        FileFull,

        FilePartial
    }

    public class TracerConfig
    {

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming",
            "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cti",
            Justification = "StreamInsight specific terminology")]
        public bool DisplayCtiEvents { get; set; }

        public TracerKind TracerKind { get; set; }

        public string TraceName { get; set; }

        public bool SingleLine { get; set; }
    }
}
