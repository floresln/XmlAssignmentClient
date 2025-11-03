using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace XmlAssignmentClient
{
    public class Program
    {

        // NOTE: spaces must be %20.
        public static string xmlURL = @"file:///Z:/Downloads/Project3Services%20(1)/Assignment4XML/Hotels.xml";        
        public static string xmlErrorURL = @"file:///Z:/Downloads/Project3Services%20(1)/Assignment4XML/HotelsErrors.xml";  
        public static string xsdURL = @"file:///Z:/Downloads/Project3Services%20(1)/Assignment4XML/Hotels.xsd";        

        public static void Main(string[] args)
        {
            Console.WriteLine("GOOD file:");
            Console.WriteLine(Verification(xmlURL, xsdURL));

            Console.WriteLine("BAD file:");
            Console.WriteLine(Verification(xmlErrorURL, xsdURL));

            Console.WriteLine("JSON output:");
            Console.WriteLine(Xml2Json(xmlURL));

        }

        public static string Verification(string xmlUrl, string xsdUrl)
        {
            var errors = new List<string>();

            var schemas = new XmlSchemaSet(); 
            using (var wc = new WebClient())
            using (var xsdStream = wc.OpenRead(xsdUrl))
            using (var xsdReader = XmlReader.Create(xsdStream))
            {
                schemas.Add(null, xsdReader); 
            }

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemas
            };

            settings.ValidationEventHandler += (s, e) =>
            {
                var ex = e.Exception;
                var where = (ex != null) ? $"line {ex.LineNumber}, pos {ex.LinePosition}: " : "";
                errors.Add($"{e.Severity}: {where}{e.Message}");
            };

            try
            {
                using (var wc = new WebClient())
                using (var xmlStream = wc.OpenRead(xmlUrl))
                using (var xsdReader = XmlReader.Create(xmlStream, settings))
                using (var reader = XmlReader.Create(xmlStream, settings))
                {
                    while (reader.Read()) { /* iterate to trigger validation */ }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"FATAL: {ex.GetType().Name}: {ex.Message}");
            }


            return errors.Count == 0 ? "No errors are found" : string.Join(Environment.NewLine, errors);
            
        }

        public static string Xml2Json(string xmlUrl)
        {
            XDocument doc;
            using (var wc = new WebClient())
            using (var xmlStream = wc.OpenRead(xmlUrl))
            using (var sr = new System.IO.StreamReader(xmlStream))
            {
                doc = XDocument.Parse(sr.ReadToEnd());
            }

            var jHotels = new JObject();
            var jHotelArray = new JArray(); 

            foreach (var h in doc.Root.Elements("Hotel"))
            {
                var jH = new JObject
                {
                    ["Name"] = (string)h.Element("Name"),
                    ["Phone"] = new JArray(
                        h.Elements("Phone")
                            .Select(p => (string)p)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                    ),

                    ["Address"] = new JObject
                    {
                        ["Number"] = (string)h.Element("Address")?.Element("Number"),
                        ["Street"] = (string)h.Element("Address")?.Element("Street"),
                        ["City"] = (string)h.Element("Address")?.Element("City"),
                        ["State"] = (string)h.Element("Address")?.Element("State"),
                        ["Zip"] = (string)h.Element("Address")?.Element("Zip"),
                        ["NearestAirport"] = (string)h.Element("Address")?.Element("NearestAirport")
                    }

                };

                var rating = (string)h.Element("Rating");
                if (!string.IsNullOrWhiteSpace(rating))
                {
                    jH["_Rating"] = rating; // must be named "_Rating" per spec
                }

                jHotelArray.Add(jH);
            }
            jHotels["Hotel"] = jHotelArray;
            var root = new JObject { ["Hotels"] = jHotels };
            return root.ToString(Newtonsoft.Json.Formatting.None);

        }
    }
}
