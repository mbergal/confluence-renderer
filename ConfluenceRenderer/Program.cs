using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Medallion.Shell;
using Microsoft.Extensions.Caching.Memory;

namespace ConsoleApp2
{
    class Program
    {
        const string TALK_ICON =
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAACXBIWXMAAA7DAAAOwwHHb6hkAAABaUlEQVR4nGP5//8/AylAyLRMWlxMYP/LVx9UE4LMA1lI1KwpLS6wVcncVHFrgiGDSVDXeoIGADXxAikpSTH+FhcrdYuVE5JkEmaeYxCXEALLs9RM2BI+bemhFbgMAGp8r68uzViZ7iagoyYJF//2B0KzgDSfWVfGoCgjjKEZZNOMFENBGP/H3/9IBkDYYC8IiQoxfPyJPTCRNSGD7zAXgIiPv3DHBC4DUFzw8RdO/UADsIt/o9QFcC/Y2BtG+/tXLYVJ+CaHwxVtmbeKYTMQO9rpvvPzNRPS01HA9MKmnshlDAwgDI7z/5Ge+gziXIwMEkC8ee5KkDDH/oOX1G/cepL/+cv36LlzitkxvIAPvDvd9RNIXQLiZKAFmekZE+/YBnnJogQiNqDjWAmiUAIAaNgvoCH6QJe9BWJGaw+bSJwGQDMZhjzQkPdAignGx+sFoOJ/+OQxDNA21PCICqnaAWKrK0vuIaQZBABhs5JVIUSzNQAAAABJRU5ErkJggg==";

        private const string CSS = @"
body {
    color: #172b4d;
    font-family: -apple-system,BlinkMacSystemFont,""Segoe UI"",""Roboto"",""Oxygen"",""Ubuntu"",""Fira Sans"",""Droid Sans"",""Helvetica Neue"",sans-serif;
    font-size: 14px;
    font-weight: 400;
    line-height: 1.42857143;
    letter-spacing: 0;
}

h1 {
    font-size: 24px;
    font-weight: normal;
    line-height: 1.25;
    margin: 30px 0 0 0;    
}

h2 {
    font-size: 20px;
    font-weight: normal;
    line-height: 1.5;
    margin: 30px 0 0 0;
}

.toc-zone {
    border: 1px solid blue; 
    background-color: #f0f0f0; 
    border: 1px solid #ddd; 
    margin: 0 2px; 
    min-height: 24px;
    padding: 10px;
}
";

        static byte[] complexObjectFactory(ICacheEntry cacheEntry)
        {
            string plantUml = (string) cacheEntry.Key;
            Console.WriteLine($"Rendering {plantUml}...");
            var cmd = Command.Run("cmd.exe", "/c plantuml -p");

            cmd.StandardInput.PipeFromAsync(new StringReader(plantUml));
            var ms = new MemoryStream();
            cmd.StandardOutput.PipeToAsync(ms, true, true).Wait();
            ms.Seek(0, SeekOrigin.Begin);
            var ss = new BinaryReader(ms);
            var bbb = ss.ReadBytes((int)ms.Length);
            Console.WriteLine($"...done");
            return bbb;
        }


        static IMemoryCache cache = new Microsoft.Extensions.Caching.Memory.MemoryCache( new MemoryCacheOptions());

        static void Main(string[] args)
        {
            var filePath = args[0];


            // Declare (but don't execute) a func/delegate whose result we want to cache

            // Get our ComplexObjects from the cache, or build them in the factory func 
            // and cache the results for next time under the given key

            var content = "";
            while (true)
            {
                var newContent = File.ReadAllText(filePath);
                if (newContent != content)
                {
                    Console.WriteLine("Change detected");
                    try
                    {
                        newMethod1(filePath);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.Message, ex.StackTrace);
                    }

                    content = newContent;
                }

                Thread.Sleep(300);
            }
        }

        private static void newMethod1(string filePath)
        {
            var xml = File.ReadAllText(filePath);
            var rootedXml = $"<root xmlns:ac=\"http://ac.com\">{xml}</root>";
            var outWriter = new StringWriter();
            var a = XDocument.Parse(rootedXml);
            var c = a;
            outWriter.WriteLine($"<style>\n{CSS}\n</style>");
            outWriter.WriteLine("<body>");

            foreach (var aa in a.Root.Elements())
            {
                newMethod(outWriter, aa);
            }

            outWriter.WriteLine("</body>");
            File.WriteAllText(filePath + ".html", outWriter.ToString());
        }

        public static string OuterXml(XElement thiz)
        {
            var xReader = thiz.CreateReader();
            xReader.MoveToContent();
            return xReader.ReadOuterXml();
        }

        private static void newMethod(TextWriter outWriter, XElement aa)
        {
            XNamespace skos = XNamespace.Get("http://ac.com");

            switch (aa.Name.LocalName)
            {
                case "h1":
                case "h2":
                case "h3":
                case "ul":
                case "p":
                case "br":
                    processElement(outWriter, aa);
                    break;
                case "code":
                {
                    outWriter.WriteLine("<span style=\"font: Courier\">");
                    processInlines(outWriter, aa);
                    outWriter.WriteLine("</span>");
                    break;
                }
                case "span":

                    outWriter.WriteLine(OuterXml(aa));
                    break;
                case "li":
                {
                    outWriter.WriteLine($"<{aa.Name.LocalName}>");
                    processInlines(outWriter, aa);
                    outWriter.WriteLine($"</{aa.Name.LocalName}>");

                    break;
                }
                case "inline-comment-marker":
                    outWriter.WriteLine(aa.Value);
                    break;
                case "structured-macro":
                    var b = aa.Attribute(skos + "name");
                    switch (b.Value)
                    {
                        case "toc-zone":
                            outWriter.WriteLine(
                                "<div class=\"toc-zone\">toc-zone</div>");
                            break;
                            ;
                        case "talk":
                            outWriter.WriteLine($"<img src=\"{TALK_ICON}\"></img>");
                            break;
                        case "plantuml":
                        {
                            var p = aa.Elements().ToArray()[1].Value;
                            byte[] cachedResults = cache.GetOrCreate(p, complexObjectFactory);

                            outWriter.WriteLine("<img src=\"data:image/png;base64," +
                                                Convert.ToBase64String(cachedResults) + "\" />");

                            break;
                        }
                        case "code":
                        {
                            var p = aa.Elements().ToArray()[2].Value;
                            outWriter.WriteLine($"<pre>{WebUtility.HtmlEncode(p)}</pre>");
                            break;
                        }
                        default:
                            throw new Exception("");
                    }

                    break;
                default:
                    throw new Exception($"Unknown {aa.Name.LocalName}");
            }
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private static void processElement(TextWriter outWriter, XElement aa)
        {
            outWriter.WriteLine($"<{aa.Name}>");
            processInlines(outWriter, aa);
            outWriter.WriteLine($"</{aa.Name}>");
        }

        private static void processInlines(TextWriter outWriter, XElement aa)
        {
            foreach (var x in aa.Nodes())
            {
                switch (x.NodeType)
                {
                    case XmlNodeType.Text:
                        outWriter.Write(x.ToString());
                        break;
                        ;
                    case XmlNodeType.Element:
                        newMethod(outWriter, (XElement) x);
                        break;
                    default:
                        throw new Exception();
                }
            }
        }
    }
}