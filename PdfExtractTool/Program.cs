using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

class PdfTextExtractor
{
    static void Main()
    {
        var bytes = File.ReadAllBytes(@D:\Code\Git\MadisonDecker\SmartOps\WorkForce Software REST API Guide.pdf);
        var raw = Encoding.Latin1.GetString(bytes);
        var streamPattern = new Regex(@stream\r?\n(.*?)endstream, RegexOptions.Singleline);
        var matches = streamPattern.Matches(raw);
        Console.Error.WriteLine(Total streams:  + matches.Count);
        var allText = new StringBuilder();
        int processed = 0;
        foreach (Match m in matches)
        {
            var streamData = Encoding.Latin1.GetBytes(m.Groups[1].Value);
            try
            {
                if (streamData.Length > 2)
                {
                    using var ms = new MemoryStream(streamData, 2, streamData.Length - 2);
                    using var ds = new DeflateStream(ms, CompressionMode.Decompress);
                    using var reader = new StreamReader(ds, Encoding.Latin1);
                    var text = reader.ReadToEnd();
                    allText.AppendLine(text);
                    processed++;
                }
            }
            catch { }
        }
        Console.Error.WriteLine(Processed:  + processed);
        File.WriteAllText(@D:\Code\Git\MadisonDecker\SmartOps\pdf_extracted.txt, allText.ToString(), Encoding.UTF8);
        Console.Error.WriteLine(Done);
    }
}
