using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Totem.Html;

namespace ConsoleApp1
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Opening HTML document sample.");
      TextReader tr = new StreamReader("./_scratchpad.html");
      var output = tr.ReadToEnd();
      var xDoc = XDocument.Parse(output);
      //var xDoc = XDocument.Load(tr);
      var mDoc = MDocumentReader.ReadMDocument(xDoc);
    }
  }
}
