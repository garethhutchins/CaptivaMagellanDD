using CaptivaMagellan;
using Emc.InputAccel.UimScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Custom.InputAccel.UimScript
{
    class CommonFunctions
    {
    }
    public class UseMagellan
    {
       
        public List<string> groceries = new List<string>();
        
        public void CallMagellan(IUimDataContext dataContext)
        {
            //First get the OCR we want to send to the service
            string OCRFT = dataContext.FindFieldDataContext("PurchasedGroceries").Text;

            if (OCRFT != "")
            {
                //Clean up the OCR
                OCRFT = ConvertLineBreaks(OCRFT);
                String xml10pattern = "[^\u0009\u000a\u000d\u0020-\ud7ff\ue000-\ufffd]|([\ud800-\udbff](?![\udc00-\udfff]))|((?<![\ud800-\udbff])[\udc00-\udfff])";
                Regex rgx = new Regex(xml10pattern);
                string ROCRFT = rgx.Replace(OCRFT, "");
                ROCRFT = xmlEscapeText(ROCRFT);
                //Need to remove ?
                ROCRFT = ROCRFT.Replace("?", "");
                //Save this back as a string for TME tesing purposes
                dataContext.FindFieldDataContext("PurchasedGroceries").SetValue(ROCRFT);
                //first build the first part of the XML
                string XMLcommand = @"<?xml version=""1.0"" encoding=""UTF-8"" ?> <Nserver><ResultEncoding>UTF-8</ResultEncoding><TextID>Captiva</TextID><NSTEIN_Text>";
                //Add the full Text
                XMLcommand = XMLcommand + ROCRFT;
                //Finish the XML
                XMLcommand = XMLcommand + @"</NSTEIN_Text><Methods><nfinder><nfFullTextSearch><Cartridges><Cartridge>Groceries</Cartridge></Cartridges></nfFullTextSearch></nfinder></Methods></Nserver>";

                //Send the request
                Connector tme = new Connector("127.0.0.1", 40000);
                XMLcommand = ConvertLineBreaks(XMLcommand);
                String result = tme.Process(XMLcommand);
                result = ConvertLineBreaks(result);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(result);
                XmlNodeList Terms = xmlDoc.GetElementsByTagName("ExtractedTerm");
                //Blank out all of the old values
                groceries.Clear();
                
                foreach (XmlNode node in Terms)
                {
                    //Get the mainTerm
                    try
                    {
                        string tt = node.Attributes["CartridgeID"].Value.ToString();
                        XmlNode child;


                        switch (tt)
                        {
                            case "Groceries":
                                child = null;
                                if (node.SelectSingleNode("MainTerm") != null)
                                {
                                    child = node.SelectSingleNode("MainTerm");


                                }
                                if (node.SelectSingleNode("nfinderNormalized") != null)
                                {
                                    child = node.SelectSingleNode("nfinderNormalized");


                                }
                                if (!groceries.Contains(child.InnerText))
                                {
                                    groceries.Add(child.InnerText);
                                }
                                break;   
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                //Check to see if any Groceries were detected
                string prodcuts = "";
                if(groceries.Count > 0)
                {
                    foreach(var prodcut in groceries)
                    {
                        if(prodcuts == "")
                        {
                            prodcuts = prodcut;
                        }
                        else
                        {
                            prodcuts = prodcuts + System.Environment.NewLine + prodcut;
                        }
                    }
                }
                //Set the Value if not blank
                if (prodcuts!="")
                {
                    dataContext.FindFieldDataContext("PurchasedGroceries").SetValue(prodcuts);
                }
                
                
            }
        }

        private string ConvertLineBreaks(string text)
        {
            if (text != null && text.IndexOf(Environment.NewLine) == -1)
            {
                if (text.IndexOf("\r\n") != -1)
                    text = text.Replace("\r\n", Environment.NewLine);
                else if (text.IndexOf("\n") != -1)
                    text = text.Replace("\n", Environment.NewLine);
                else
                    text = text.Replace("\r", Environment.NewLine);
            }
            //now replace with spaces
            //text = text.Replace(Environment.NewLine, " ");
            return text;
        }

        private String xmlEscapeText(string t)
        {

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < t.Length; i++)
            {
                char c = t[i];
                switch (c)
                {

                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '\"': sb.Append("&quot;"); break;
                    case '&': sb.Append("&amp;"); break;
                    case '\'': sb.Append("&apos;"); break;
                    default:
                        if (c > 0x7e)
                        {
                            sb.Append("&#" + ((int)c) + ";");
                        }
                        else { sb.Append(c); }
                        break;
                }
            }
            return sb.ToString();
        }

        
    }
}
