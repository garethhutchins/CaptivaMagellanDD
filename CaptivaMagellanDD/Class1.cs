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
    using Emc.InputAccel.CaptureClient;
    using System.Windows.Forms;
    using System.IO;
    using Newtonsoft.Json;

    public sealed class ScriptMain : UimScriptMainCore
    {
    }

    /// <summary>
    /// The custom script class for the document type 
    /// <Document Type Name as defined in Captiva Designer>.
    ///</summary>
    public class ScriptIris : UimScriptDocument
    {
        public static string MDDurl = "http://magellandd:8110/restapi/rest";
        //Declaare a string to save the Decision Tree
        public string DTJSON = "";

        /// <summary>
        /// Executes when the Document is first loaded for the task by the Completion module, 
        /// after all of the runtime setup is complete.
        /// </summary>
        /// <param name="dataContext">The context object for the document.</param>
        public void DocumentLoad(IUimDataContext dataContext)
        {
            //First of all, get the documenttype name
            string DocType = dataContext.DocumentName;
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
                                            if (y.name == DocType)
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
            using (HttpWebResponse response = (HttpWebResponse)DTRequest.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    // Get a reader capable of reading the response stream
                    using (StreamReader myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        string responseJSON = myStreamReader.ReadToEnd();
                        DTJSON = responseJSON;
                        //save the result
                        
                    }
                }
            }

           
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
        public void ExecutePopulationRuleMagellanDD(IUimDataContext dataContext)
        {
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
            Console.WriteLine("Here");
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