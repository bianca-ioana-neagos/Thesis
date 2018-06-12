using System;
using System.IO;

namespace OutputAdapter
{
    internal static class TracerRouter
    {
        public static Action<string> GetHandler(TracerKind tracerKind, string traceName)
        {
            switch (tracerKind)
            {
                
                case TracerKind.FileFull:
                    {
                        FileTracer ft = new FileTracer("results/result.txt");
                        return ft.WriteLine;
                    }
                case TracerKind.FilePartial:
                {
                    FileTracer ft = new FileTracer("results2/result" + new Random().Next(100) + ".txt");
                    return ft.WriteLine;
                }
                default:
                    return Console.WriteLine;
            }
        }

        private static void WriteMessageToTrace(string msg)
        {
            System.Diagnostics.Trace.WriteLine(msg);
            System.Diagnostics.Trace.Flush();
        }

        private sealed class FileTracer
        {
            private readonly string _fileName;

            public FileTracer(string fileName)
            {
                this._fileName = fileName;
            }

            public void WriteLine(string msg)
            {
                using (TextWriter tw = new StreamWriter(this._fileName, true))
                {
                    tw.WriteLine(msg);
                }
            }
        }
    }
}
