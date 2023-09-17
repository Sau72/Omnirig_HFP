// ************************************************************
// OmniRig demo Visual Studio Community 2017 WPF C# .NET      *
// Written in December 2018 by Björn Ekelund SM7IUN           *
// Based on original code by Stefano Pranzo HB9DNI            *
// Requires a proper reference to OmniRig COM objects to      *
// build typically the file OmniRig.exe normally located in   *
// C:\Program Files (x86)\Afreet\OmniRig                      *
// Add with right-click on "References" in Solution Explorer. *
// ************************************************************  

using System;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Threading;

namespace Omnirig_Demo_WPF_CS
{
    // Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        
        #region Constants

        // Constants for enum RigParamX
        private const int PM_UNKNOWN = 0x00000001;
        private const int PM_FREQ = 0x00000002;
        private const int PM_FREQA = 0x00000004;
        private const int PM_FREQB = 0x00000008;
        private const int PM_PITCH = 0x00000010;
        private const int PM_RITOFFSET = 0x00000020;
        private const int PM_RIT0 = 0x00000040;
        private const int PM_VFOAA = 0x00000080;
        private const int PM_VFOAB = 0x00000100;
        private const int PM_VFOBA = 0x00000200;
        private const int PM_VFOBB = 0x00000400;
        private const int PM_VFOA = 0x00000800;
        private const int PM_VFOB = 0x00001000;
        private const int PM_VFOEQUAL = 0x00002000;
        private const int PM_VFOSWAP = 0x00004000;
        private const int PM_SPLITON = 0x00008000;
        private const int PM_SPLITOFF = 0x00010000;
        private const int PM_RITON = 0x00020000;
        private const int PM_RITOFF = 0x00040000;
        private const int PM_XITON = 0x00080000;
        private const int PM_XITOFF = 0x00100000;
        private const int PM_RX = 0x00200000;
        private const int PM_TX = 0x00400000;
        private const int PM_CW_U = 0x00800000;
        private const int PM_CW_L = 0x01000000;
        private const int PM_SSB_U = 0x02000000;
        private const int PM_SSB_L = 0x04000000;
        private const int PM_DIG_U = 0x08000000;
        private const int PM_DIG_L = 0x10000000;
        private const int PM_AM = 0x20000000;
        private const int PM_FM = 0x40000000;

        // Constants for enum RigStatusX
        private const int ST_NOTCONFIGURED = 0x00000000;
        private const int ST_DISABLED = 0x00000001;
        private const int ST_PORTBUSY = 0x00000002;
        private const int ST_NOTRESPONDING = 0x00000003;
        private const int ST_ONLINE = 0x00000004;

        #endregion

        #region Properties

        // RX
        public const string FLD_RX = "RX";
        private string mRX;
        public string RX {
            get {
                return mRX;
            }
            set {
                mRX = value;
                OnPropertyChanged(FLD_RX);
            }
        }

        // TX
        public const string FLD_TX = "TX";
        private string mTX;
        public string TX {
            get {
                return mTX;
            }
            set {
                mTX = value;
                OnPropertyChanged(FLD_TX);
            }
        }

        // Frequency1
        public const string FLD_Frequency1 = "Frequency1";
        private string mFrequency1;
        public string Frequency1 {
            get {
                return mFrequency1;
            }
            set {
                mFrequency1 = value;
                OnPropertyChanged(FLD_Frequency1);
            }
        }

        // Frequency2
        public const string FLD_Frequency2 = "Frequency2";
        private string mFrequency2;
        public string Frequency2
        {
            get
            {
                return mFrequency2;
            }
            set
            {
                mFrequency2 = value;
                OnPropertyChanged(FLD_Frequency2);
            }
        }

        // Status
        public const string FLD_Status = "Status";
        private string mStatus;
        public string Status {
            get {
                return mStatus;
            }
            set {
                mStatus = value;
                OnPropertyChanged(FLD_Status);
            }
        }

        // Mode
        public const string FLD_Mode = "Mode";
        private string mMode;
        public string Mode {
            get {
                return mMode;
            }
            set {
                mMode = value;
                OnPropertyChanged(FLD_Mode);
            }
        }

        #endregion

        #region Field

        // The events subscribed
        private bool EventsSubscribed = false;

        // The omni rig engine
        OmniRig.OmniRigX OmniRigEngine;

        // The rig
        OmniRig.RigX Rig;

        // Our rig no
        int CurrentRigNo;

        // The first thread
        Thread thread1;

        // The second thread 
        Thread thread2;

        // The second thread 
        Thread thread3;

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Frequency1 = Convert.ToString(3745000);
            Frequency2 = Convert.ToString(7175000);
            StartOmniRig();
        }

        private void Window_UnLoaded(object sender, RoutedEventArgs e)
        {
            thread1.Abort();
            thread2.Abort();
            thread3.Abort();
        }

        private void StartOmniRig()
        {
            try
            {
                if (OmniRigEngine != null)
                    MessageBox.Show("OmniRig runs");
                else
                {
                    OmniRigEngine = (OmniRig.OmniRigX)Activator.CreateInstance(Type.GetTypeFromProgID("OmniRig.OmniRigX"));
                    // we want OmniRig interface V.1.1 to 1.99
                    // as V2.0 will likely be incompatible  with 1.x
                    if (OmniRigEngine.InterfaceVersion < 0x101 && OmniRigEngine.InterfaceVersion > 0x299)
                    {
                        OmniRigEngine = null;
                        MessageBox.Show("OmniRig is not installed or has unsupported version.");
                    }
                    SubscribeToEvents();
                    SelectRig(1);                    
                    FreqChange(1);
                    //TimerStart();

                }
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show(ex.Message);
                throw;
            }
        }

        

        private void SelectRig(int NewRigNo)
        {
            if (OmniRigEngine == null)
                return;

            CurrentRigNo = NewRigNo;
            switch (NewRigNo)
            {
                case 1:
                    Rig = OmniRigEngine.Rig1;
                    break;
                case 2:
                    Rig = OmniRigEngine.Rig2;
                    break;
            }
            ShowRigStatus();
            ShowRigParams();
        }

        private void ShowRigStatus()
        {
            if (Rig != null)
                Status = Rig.StatusStr;
        }

        private void RigFreqChange()
        {
            //int Freq1 = 3745000;
            //int Freq2 = 7175000;
            int freq;
            while (true)
            {
                if (Rig != null)
                {
                    if (DateTime.Now.Minute > 30)
                    {
                        freq = Convert.ToInt32(Frequency2);
                        Rig.SetSimplexMode(freq);
                    }
                    else
                    {
                        freq = Convert.ToInt32(Frequency1);
                        Rig.SetSimplexMode(freq);
                    }
                    System.Threading.Thread.Sleep(30000);
                }
            }
        }

       
        private void ShowRigParams()
        {
            if (Rig == null)
                return;

            RX = Rig.GetRxFrequency().ToString();
            TX = Rig.GetTxFrequency().ToString();
            //Frequency1 = Rig.Freq.ToString();
            switch (Rig.Mode)
            {
                case (OmniRig.RigParamX)PM_CW_L:
                    Mode = "CW";
                    break;
                case (OmniRig.RigParamX)PM_CW_U:
                    Mode = "CW-R";
                    break;
                case (OmniRig.RigParamX)PM_SSB_L:
                    Mode = "LSB";
                    break;
                case (OmniRig.RigParamX)PM_SSB_U:
                    Mode = "USB";
                    break;
                case (OmniRig.RigParamX)PM_FM:
                    Mode = "FM";
                    break;
                case (OmniRig.RigParamX)PM_AM:
                    Mode = "AM";
                    break;
                default:
                    Mode = "Other";
                    break;
            }
        }

        private void BtnOmniRigSettings_Click(object sender, RoutedEventArgs e)
        {
            if (OmniRigEngine != null)
                OmniRigEngine.DialogVisible = true;
        }
               

        // Event raised when property changed
        public event PropertyChangedEventHandler PropertyChanged;

        // Event handler for property changed
        // <param name="pPropertyName">Name of property</param>
        protected virtual void OnPropertyChanged(string pPropertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(pPropertyName));
        }

        private void SubscribeToEvents()
        {
            if (!EventsSubscribed)
            {
                EventsSubscribed = true;
                OmniRigEngine.StatusChange += OmniRigEngine_StatusChange;
                OmniRigEngine.ParamsChange += OmniRigEngine_ParamsChange;                
            }
        }

        //OmniRig ParamsChange events
        private void OmniRigEngine_ParamsChange(int RigNumber, int Params)
        {
            if (RigNumber == CurrentRigNo)
            {
                thread1 = new Thread(new ThreadStart(ShowRigParams));
                thread1.Name = "RigParams";
                //Start first thread
                thread1.Start();  
            }

        }
        
        //OmniRig StatusChange events
        private void OmniRigEngine_StatusChange(int RigNumber)
        {
            if (RigNumber == CurrentRigNo)
            {
                thread2 = new Thread(new ThreadStart(ShowRigStatus));
                thread2.Name = "RigStatus";
                //Start second thread
                thread2.Start();
            }
        }

        //OmniRig FreqChange events
        private void FreqChange(int RigNumber)
        {
            if (RigNumber == CurrentRigNo)
            {
                thread3 = new Thread(new ThreadStart(RigFreqChange));
                thread3.Name = "FreqChange";
                //Start second thread
                thread3.Start();
            }
        }
             
               
    }
}