using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emc.InputAccel.UimScript;
using System.Net;
using Microsoft.CSharp.RuntimeBinder;

namespace Custom.InputAccel.UimScript
{
    using Emc.InputAccel.UimScript;
    using System.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Emc.InputAccel.CaptureClient;
    using System.Text.RegularExpressions;
    using System.Globalization;

    public sealed class ScriptMain : UimScriptMainCore
    {
    }

    /// <summary>
    /// The custom script class for the document type 
    /// <Document Type Name as defined in Captiva Designer>.
    ///</summary>
    ///
    public class ScriptGroceries : UimScriptDocument
    {
        public void DocumentLoad(IUimDataContext dataContext)
        {

        }

        /// <summary>
        /// Executes when the Document is first loaded for the task by the Extraction 
        /// module, after all of the runtime setup is complete.
        ///</summary>
        /// <param name="dataContext">The context object for the document.</param>
        public void DocumentUnload(IUimDataContext dataContext)
        {
        }
        public void ExecutePopulationRuleMagellanDDAR(IUimDataContext dataContext)
        {
            //Call the TME Engine
            UseMagellan CF = new UseMagellan();
            CF.CallMagellan(dataContext);
        }
        public void ExecutePopulationRuleGetRecommendations(IUimDataContext dataContext)
        {
            //Execute the DB Lookup
            Dictionary<string, object> EmptyDict = new Dictionary<string, object>();
            ITableResults res = dataContext.LookupData("GetRecommendations", EmptyDict);
            //Now loop through all of the results
            //See if each row contains has of the values in the purchased field by splitting them up
            //If they do then it's a match
            string PGoods = dataContext.FindFieldDataContext("PurchasedGroceries").Text;
            PGoods = PGoods.Replace(System.Environment.NewLine, "|");
            //Changed the extracted item to lowercase to match the DB
            PGoods = PGoods.ToLower();
            char[] c = "|".ToCharArray();
            string[] Purchased = PGoods.Split(c[0]);
            List<String> RecomendList = new List<string>();
            //Now loop through the rows
            for (int i = 0; i < res.RowCount; i++)
            {
                //Get the values from the row
                ITableRow TR = res.ElementAt(i);
                string PG = TR.AsString(0);
                PG = PG.Replace(@"""", "");
                string RG = TR.AsString(1);
                RG = RG.Replace(@"""", "");
                //Next split each into an array and see if they are in the purchased goods field
                char[] c2 = ",".ToCharArray();
                //Convert PG to Lowercase
                PG = PG.ToLower();
                string[] RowPurchased = PG.Split(c2[0]);
                string[] Recomended = RG.Split(c2[0]);
                Boolean inList = true;
                foreach(var item in RowPurchased)
                {
                    if (!Purchased.Contains(item))
                    {
                        inList = false;
                    }
                }
                //Now see if the values were extracted for that table
                if (inList)
                {
                    foreach(var item in Recomended)
                    {
                        if(!RecomendList.Contains(item) && !Purchased.Contains(item))
                        {
                            RecomendList.Add(item);
                        }
                    }
                }
            }
            //Now convert the list to string
            string OutValue = "";
            TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
            foreach (string val in RecomendList)
            {
                if(OutValue == "")
                {
                    OutValue = ti.ToTitleCase(val);
                }
                else
                {
                    OutValue = OutValue + System.Environment.NewLine + ti.ToTitleCase(val);
                }
            }
            //Now Send the Value back to the fiels
            dataContext.FindFieldDataContext("RecommendedGroceries").SetValue(OutValue);
        }
    }
    public class ScriptIris : UimScriptDocument
    {
        public static string MDDurl = "http://magellandd:8110/restapi/rest";

        
        /// <summary>
        /// Executes when the Document is first loaded for the task by the Completion module, 
        /// after all of the runtime setup is complete.
        /// </summary>
        /// <param name="dataContext">The context object for the document.</param>
        /// 
       
        public string GetDT(string DocumentType)
        {
            //Create a string to save the Decision Tree ID
            string TreeID = "";
            //Now query the Magellan DD web service to get a list of all of the folders and objects in the root
            string MDDFold = MDDurl + "/folders/id?username=Administrator&password=PASSWORD&folderID=1";
            //Now do the post
            HttpWebRequest FolderRequest = (HttpWebRequest)WebRequest.Create(MDDFold);
            FolderRequest.Method = "POST";
            //Now get the response back
            using (HttpWebResponse response = (HttpWebResponse)FolderRequest.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    // Get a reader capable of reading the response stream
                    using (StreamReader myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        // Read stream content as string
                        string responseJSON = myStreamReader.ReadToEnd();

                        // Assuming the response is in JSON format, deserialize it
                        // creating an instance of TData type (generic type declared before).
                        dynamic res = JsonConvert.DeserializeObject(responseJSON);
                        //Now loop through all of the childern
                        foreach (var x in res)
                        {
                            try
                            {

                                if (x.Name != null)
                                {
                                    //Now check to see if it's a match
                                    if (x.Name == "a")
                                    {
                                        foreach (var y in x)
                                        {
                                            if (y.name == DocumentType)
                                            {
                                                //Get the ID
                                                TreeID = y.id;
                                            }

                                        }

                                    }
                                }
                            }
                            catch (RuntimeBinderException)
                            {

                            }

                        }
                    }
                }

            }
            //Now sent another request to get the DecisionTree
            string MDDDT = MDDurl + "/analysis/id?username=Administrator&password=PASSWORD&repository=DEMO&analysisID=" + TreeID + "&pageNumber=1";
            HttpWebRequest DTRequest = (HttpWebRequest)WebRequest.Create(MDDDT);
            DTRequest.Method = "POST";
            //Declaare a string to save the Decision Tree
            //Now get the response back
            string DTJSON = "";
            using (HttpWebResponse response = (HttpWebResponse)DTRequest.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    // Get a reader capable of reading the response stream
                    using (StreamReader myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        string responseJSON = myStreamReader.ReadToEnd();
                        DTJSON = responseJSON;
                    }
                }
            }
            return DTJSON;
        }
        public void GetDecision(IUimDataContext dataContext)
        {
            //First of all, get the documenttype name
            string DocType = dataContext.DocumentName;
            //Now get the Decision Tree back
            string StrDecisionTree = GetDT(DocType);
            //Load the decision tree nodes
            dynamic DecisionTree = JsonConvert.DeserializeObject(StrDecisionTree);
            //Now loop through the tree and find the decisions that need to be made
            int DCount = 0;
            Boolean ValueSet = false;
            string[] attributes = new string[4];
            foreach (var Decision in DecisionTree)
            {
                if (ValueSet == true) { break; }

                if (DCount < 4)
                {
                    string[] tname;
                    tname = Decision.ToString().Split('.');
                    //Get the last one
                    attributes[DCount] = tname[tname.Length - 1];
                    //remove the []
                    attributes[DCount] = attributes[DCount].Replace("[", "");
                    attributes[DCount] = attributes[DCount].Replace("]", "");
                }
                int tCount = 0;
                Boolean DOK = false;
                string fVal = "";
                double dVal = 0;
                foreach (JToken token in Decision.Children())
                {
                    //Get the opperator
                    string tValue = token.ToString();
                    //Check to see the class and if alls is OK
                    if (DOK == true && tCount == 4)
                    {
                        //Then it's this class
                        dataContext.FindFieldDataContext("PredictedClass").SetValue(tValue);
                        ValueSet = true;
                        break;
                    }
                    if (tValue != "" && tCount < 4)
                    {
                        string[] splitValue = tValue.Split(' ');
                        //check the opperator

                        switch (splitValue[0])
                        {
                            case "<=":
                                //Now see what field we need to test based on the tcoung

                                fVal = dataContext.FindFieldDataContext(attributes[tCount]).Value.ToString();
                                if (double.TryParse(fVal, out dVal))
                                {
                                    if (Convert.ToDouble(dataContext.FindFieldDataContext(attributes[tCount]).Value) <= Convert.ToDouble(splitValue[1]))
                                    {
                                        //Then this one is true
                                        DOK = true;
                                    }
                                    else
                                    {
                                        DOK = false;
                                        //This will exit the foreach loop too.
                                        break;
                                    }
                                }
                                break;
                            case "<":
                                //Now see what field we need to test based on the tcoung
                                fVal = dataContext.FindFieldDataContext(attributes[tCount]).Value.ToString();
                                if (double.TryParse(fVal, out dVal))
                                {
                                    if (Convert.ToDouble(dataContext.FindFieldDataContext(attributes[tCount]).Value) < Convert.ToDouble(splitValue[1]))
                                    {
                                        //Then this one is true
                                        DOK = true;
                                    }
                                    else
                                    {
                                        DOK = false;
                                        //This will exit the foreach loop too.
                                        break;
                                    }
                                }
                                break;
                            case "=":
                                fVal = dataContext.FindFieldDataContext(attributes[tCount]).Value.ToString();
                                if (double.TryParse(fVal, out dVal))
                                {
                                    if (Convert.ToDouble(dataContext.FindFieldDataContext(attributes[tCount]).Value) == Convert.ToDouble(splitValue[1]))
                                    {
                                        //Then this one is true
                                        DOK = true;
                                    }
                                    else
                                    {
                                        DOK = false;
                                        //This will exit the foreach loop too.
                                        break;
                                    }
                                }
                                break;
                            case ">":
                                fVal = dataContext.FindFieldDataContext(attributes[tCount]).Value.ToString();
                                if (double.TryParse(fVal, out dVal))
                                {
                                    if (Convert.ToDouble(dataContext.FindFieldDataContext(attributes[tCount]).Value) > Convert.ToDouble(splitValue[1]))
                                    {
                                        //Then this one is true
                                        DOK = true;
                                    }
                                    else
                                    {
                                        DOK = false;
                                        //This will exit the foreach loop too.
                                        break;
                                    }
                                }
                                break;
                            case ">=":
                                fVal = dataContext.FindFieldDataContext(attributes[tCount]).Value.ToString();
                                if (double.TryParse(fVal, out dVal))
                                {
                                    if (Convert.ToDouble(dataContext.FindFieldDataContext(attributes[tCount]).Value) >= Convert.ToDouble(splitValue[1]))
                                    {
                                        //Then this one is true
                                        DOK = true;
                                    }
                                    else
                                    {
                                        DOK = false;
                                        //This will exit the foreach loop too.
                                        break;
                                    }
                                }
                                break;

                        }
                    }
                    //Increase the Count by 1
                    tCount++;
                }

                DCount++;
            }
        }
        public void DocumentLoad(IUimDataContext dataContext)
        {
           
        }

        /// <summary>
        /// Executes when the Document is first loaded for the task by the Extraction 
        /// module, after all of the runtime setup is complete.
        ///</summary>
        /// <param name="dataContext">The context object for the document.</param>
        public void DocumentUnload(IUimDataContext dataContext)
        {
        }
        /// <summary>
        /// Executes a named validation rule.
        /// </summary>
        /// <param name="dataContext">The context object for the document.</param>
        public void ExecutePopulationRuleMagellanDDDT(IUimDataContext dataContext)
        {
            GetDecision(dataContext);
        }

        /// <summary>
        /// Executes a named validation rule.
        /// </summary>
        /// <param name="dataContext">The context object for the document.</param>
        public void ExecuteValidationRule<rulename>(IUimDataContext dataContext)
        {

        }
        
        /// <summary>
        /// Executes when the data entry form is loaded.
        /// </summary>
        /// <param name="controlContext">The context object for the data entry form.</param>
        public void FormLoad(IUimDataEntryFormContext formContext)
        {
            
        }

        /// <summary>
        /// Executes when a UI control gains focus.
        /// </summary>
        /// <param name="controlContext">The context object for the control.</param>
        public void EnterControl(IUimFormControlContext controlContext)
        {
        }

        /// <summary>
        /// Executes when a UI control loses focus.
        /// </summary>
        /// <param name="controlContext">The context object for the control.</param>
        public void ExitControl(IUimFormControlContext controlContext)
        {
        }

        /// <summary>
        /// Executes when the operator confirms the field value by pressing Enter.
        /// </summary>
        /// <param name="controlContext">The context object for the control.</param>
        public void ConfirmControl(IUimFormControlContext controlContext)
        {
        }


        /// <summary>
        /// Executes when a button is clicked.
        /// </summary>
        /// <param name="controlContext">The context object for the control.</param>
        public void ButtonClick(IUimFormControlContext controlContext)
        {
        }
    }
}