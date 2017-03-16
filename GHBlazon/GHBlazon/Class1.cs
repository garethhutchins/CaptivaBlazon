using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emc.InputAccel.CaptureClient;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Threading;

namespace GHBlazon
{
    public class GHBlazon : CustomCodeModule
    {
        private string URL;
        private string DIR;
        private int WaitTime;
        private bool CleanUp;
        private IClientTask CurrentTask;
        private IBatchNode CurrentFile;
        

        public override void ExecuteTask(IClientTask task, IBatchContext batchContext)
        {
            URL = task.BatchNode.StepData.StepConfiguration.ReadString("BlazonURL");
            DIR = task.BatchNode.StepData.StepConfiguration.ReadString("BlazonTempDIR");
            WaitTime = task.BatchNode.StepData.StepConfiguration.ReadInt("WaitTime",5);
            CleanUp = task.BatchNode.StepData.StepConfiguration.ReadBoolean("CleanUp", false);
            CurrentTask = task;
            try
            {
                //First read the file from the Batch
                if (task.BatchNode.RootLevel != 0)
                {
                    //throw new System.ArgumentException("Module should run at Page Level");
                    IBatchNodeCollection pages =  task.BatchNode.GetDescendantNodes(0);
                    int numpages = pages.Count();
                    int i = 0;
                    while (i < numpages)
                    {
                        IBatchNode page = pages[i];
                        SendFileToBlazon(page);
                        i++;
                    }
                    task.CompleteTask();
                }
                else
                {
                    SendFileToBlazon(task.BatchNode);
                    task.CompleteTask();
                }

            }
            catch (Exception e)
            {
                task.FailTask(FailTaskReasonCode.GenericRecoverableError, e);
            }
           
        }
        public void SendFileToBlazon(IBatchNode taskFile)
        {
            CurrentFile = taskFile;
            //Try to see if there's an inputfile type
            string FileType = "";
            
            FileType = taskFile.NodeData.ValueSet.ReadString("InputFileType");
            if (FileType == "")
            {
                throw new System.Exception("InputFileType not found");
            }
            //Now Set the File Name
            string TempFile = DIR + "/" + taskFile.NodeData.BatchId + "_" + taskFile.NodeData.NodeId  + FileType;
            //Now read the data to a file
            taskFile.NodeData.ValueSet.ReadFile("InputFile").ReadToFile(TempFile);
            //Now call the Blazon service
            string BlazonCommand = URL + "/QueueServer/push.aspx?source=" + TempFile + "&target=" + DIR + "&outputformat=tif";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(BlazonCommand);
            request.GetResponse();
            //Now wait untill the file is in the output directory
            string newfile = DIR + "/" + taskFile.NodeData.BatchId + "_" + taskFile.NodeData.NodeId + ".tif";
            DateTime StartTime = DateTime.Now;
           
            while (!File.Exists(newfile))
            {
                
                int HowLong = DateTime.Now.Subtract(StartTime).Seconds;
                if (HowLong < WaitTime)
                {
                    Thread.Sleep(100);
                }
                else
                {
                    //This is taking too long
                    throw new System.TimeoutException("The Blazon Service took too long so I gave up"); 
                }

                
            }
            bool canopen = false;
            while (! canopen == true )
            {
                //Check to see hoe long we've waited
                int HowLong = DateTime.Now.Subtract(StartTime).Seconds;
                
                if (HowLong > WaitTime)
                {
                    //This is taking too long
                    throw new System.TimeoutException("The Blazon Service took too long so I gave up");
                }
                else
                {
                    try
                    {
                        using (File.Open(newfile, FileMode.Open)) { canopen = true; }
                    }
                    catch (IOException e)
                    {
                        Thread.Sleep(100);
                        e.Data.Clear();

                    }
                }
            
            } 
            byte[] f = File.ReadAllBytes(newfile);
            CurrentFile.NodeData.ValueSet.WriteFileData("OutputFile", f, ".tif");
            CurrentFile.NodeData.ValueSet.WriteString("OutputFileType", "tif");
            //Now delete the files
            if (CleanUp == true)
            {
                File.Delete(TempFile);
                File.Delete(newfile);
            }
            
        }
     

        public override void StartModule(ICodeModuleStartInfo startInfo)
        {
            
            startInfo.Trace("Module Started");
        }
        public override bool SetupCodeModule(Control parentWindow, IValueAccessor stepConfiguration)
        {
            bool Saved = false;
            //Show the Setup Form
            SetupForm a = new SetupForm(stepConfiguration);
            a.Show();
            return Saved; 
        }
    }
}
