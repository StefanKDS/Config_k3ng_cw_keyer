using CommunityToolkit.Mvvm.Input;
using k3ng_cw_keyer_config.Model;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;

namespace k3ng_cw_keyer_config.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region Fields
    private string _selectedPort = "";
    private readonly SerialPort _serialPort;
    private ConfigItem? _selectedConfigItem;
    private string _outputText;
    private string _inputText;
    private int _outputCaretIndex;
    private string _parameterString;
    private FavItem _selectedFavItem;
    #endregion

    #region Konstruktor / Destruktor
    public MainViewModel()
    {
        this.PortList = new ObservableCollection<string>();
        this.CommandList = new ObservableCollection<ConfigItem>();
        this.FavList = new ObservableCollection<FavItem>();
        this._serialPort = new SerialPort();

        this.ClearOutputCommand = new RelayCommand(this.OnClearOutput);
        this.SendCommand = new RelayCommand(this.OnSend);
        this.SendCmdCommand = new RelayCommand(this.OnSendCommand);
        this.AddFavCommand = new RelayCommand(this.OnAddFav);
        this.RemoveFavItemCommand = new RelayCommand(this.OnRemoveFavItem);

        this.ListAlAvailablePorts();
        this.InitCommandList();

        this.LoadFavData();
    }

    ~MainViewModel()
    {
        if (this._serialPort.IsOpen)
        {
            this._serialPort.Close();
        }
    }
    #endregion

    #region Properties

    public ObservableCollection<string> PortList { get; set; }

    public ObservableCollection<ConfigItem> CommandList { get; set; }

    public ObservableCollection<FavItem> FavList { get; set; }

    public string OutputText 
    { 
        get
        {
            return this._outputText;
        }
        set
        {
            this._outputText = value;
            OnPropertyChanged();
        }
    }

    public string InputText
    {
        get
        {
            return this._inputText;
        }
        set
        {
            this._inputText = value;
            OnPropertyChanged();
        }
    }

    public bool IsAutoScrollChecked { get; set; }

    public bool IsClearBeforeReceive { get; set; }

    public int OutputCaretIndex
    {
        get
        {
            return this._outputCaretIndex;
        }
        set
        {
            this._outputCaretIndex = value;
            OnPropertyChanged();
        }
    }

    public string ParameterString
    {
        get
        {
            return this._parameterString;
        }
        set
        {
            this._parameterString = value;
            OnPropertyChanged();
        }
    }

    public string SelectedPort
    {
        get
        {
            return this._selectedPort;
        }
        set
        {
            if (value != this._selectedPort)
            {
                this._selectedPort = value;
                PortChanged();
            }
        }
    }

    public ConfigItem? SelectedCommand
    {
        get
        {
            return this._selectedConfigItem;
        }
        set
        {
            if (value != this._selectedConfigItem)
            {
                this.ParameterString = string.Empty;
                this._selectedConfigItem = value;
            }
        }
    }

    public FavItem? SelectedFavItem
    {
        get
        {
            return this._selectedFavItem;
        }
        set
        {
            if (value != this._selectedFavItem)
            {
                this._selectedFavItem = value;
            }
        }
    }

    #endregion

    #region Commands
    public ICommand ClearOutputCommand { get; }

    public ICommand SendCommand { get; }

    public ICommand SendCmdCommand { get; }

    public ICommand AddFavCommand { get; }

    public ICommand RemoveFavItemCommand { get; }

    private void OnClearOutput()
    {
        this.OutputText = string.Empty;
    }

    private void OnRemoveFavItem()
    {
        if (this.SelectedFavItem == null)
        {
            return;
        }

        this.FavList.Remove(this.SelectedFavItem);

        this.SaveToStream();
    }

    private void OnSend()
    {
        this._serialPort.Write(string.Format("{0}\r\n", this.InputText));
        this.InputText = string.Empty;

        if (this.IsClearBeforeReceive == true)
        {
            this.OnClearOutput();
        }
    }

    private async void OnAddFav()
    {
        if (this.SelectedCommand == null)
        {
            return;
        }

        if (this.SelectedCommand.HasParameter)
        {
            int nbrOfDigits = ExtractNbrOfDigits(this.SelectedCommand.Command);

            if (this.ParameterString.Length != nbrOfDigits)
            {
                MsBox.Avalonia.Base.IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("Error", "Wrong parameter length", ButtonEnum.Ok);
                await box.ShowAsync();
                return;
            }
        }

        this.FavList.Add(new FavItem(SendFav, this.SelectedCommand, this.ParameterString));
        this.SaveToStream();
    }

    private async void OnSendCommand()
    {
        if (this.SelectedCommand == null)
        {
            return;
        }

        if (this.SelectedCommand.HasParameter)
        {
            int nbrOfDigits = ExtractNbrOfDigits(this.SelectedCommand.Command);

            if (this.ParameterString.Length != nbrOfDigits)
            {
                MsBox.Avalonia.Base.IMsBox<ButtonResult> box = MessageBoxManager.GetMessageBoxStandard("Error", "Wrong parameter length", ButtonEnum.Ok);
                await box.ShowAsync();
                return;
            }

            string cmd = this.SelectedCommand.Command.Remove(this.SelectedCommand.Command.Length - nbrOfDigits, nbrOfDigits);
            cmd += this.ParameterString;
            if (this._serialPort.IsOpen)
            {

                if (this.IsClearBeforeReceive == true)
                {
                    this.OnClearOutput();
                }

                this._serialPort.Write(string.Format("{0}\r\n", cmd));
            }
        }
        else
        {
            if (this._serialPort.IsOpen)
            {

                if (this.IsClearBeforeReceive == true)
                {
                    this.OnClearOutput();
                }
                this._serialPort.Write(string.Format("{0}\r\n", this.SelectedCommand.Command));
            }
        }
    }
    #endregion

    #region Helper
    private void SendFav(string cmd)
    {
        if (this._serialPort.IsOpen)
        {

            if (this.IsClearBeforeReceive == true)
            {
                this.OnClearOutput();
            }

            this._serialPort.Write(string.Format("{0}\r\n", cmd));
        }
    }

    private static int ExtractNbrOfDigits(string parameter)
    {
        return parameter.Count(c => c == '#');
    }

    private void ListAlAvailablePorts()
    {
        // Display each port name to the console.
        foreach (string port in SerialPort.GetPortNames())
        {
            this.PortList.Add(port);
        }
    }

    private void PortChanged()
    {
        if (this._serialPort.IsOpen)
        {
            this._serialPort.Close();
        }

        this._serialPort.BaudRate = 115200;
        this._serialPort.DataBits = 8;
        this._serialPort.Parity = Parity.None;
        this._serialPort.PortName = this._selectedPort;
        this._serialPort.StopBits = StopBits.One;
        this._serialPort.DataReceived += this.SerialPort_DataReceived;

        this._serialPort.Open();
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort sp = (SerialPort)sender;
        string s = sp.ReadExisting();

        this.OutputText += s;

        if (this.IsAutoScrollChecked)
        {
            this.OutputCaretIndex = int.MaxValue;
        }
        else
        {
            this.OutputCaretIndex = 0;
        }
    }

    private void SaveToStream()
    {
        XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<FavItem>));
        using (StreamWriter wr = new StreamWriter("favdata.xml"))
        {
            xs.Serialize(wr, FavList);
        }
    }

    private void LoadFavData()
    {
        if (!File.Exists("favdata.xml"))
        {
            return;
        }

        XmlSerializer xs = new XmlSerializer(typeof(ObservableCollection<FavItem>));
        using (StreamReader rd = new StreamReader("favdata.xml"))
        {
            var tempList = xs.Deserialize(rd) as ObservableCollection<FavItem>;
            if (tempList != null)
            {
                foreach (var item in tempList)
                {
                    FavList.Add(new FavItem(SendFav,item.ConfigItem, item.Parameter));
                }
            }
        }
    }

    private void InitCommandList()
    {
        this.CommandList.Add(new ConfigItem("Help", @"\?"));
        this.CommandList.Add(new ConfigItem("Paged Help", @"\/"));
        this.CommandList.Add(new ConfigItem("Play memory #", @"\#", true));
        this.CommandList.Add(new ConfigItem("Iambic A mode", @"\a"));
        this.CommandList.Add(new ConfigItem("Iambic B mode", @"\b"));
        this.CommandList.Add(new ConfigItem("Switch to CW", @"\c"));
        this.CommandList.Add(new ConfigItem("Set serial number to ####", @"\e####", true));
        this.CommandList.Add(new ConfigItem("Set sidetone frequency to #### hertz", @"\f####", true));
        this.CommandList.Add(new ConfigItem("Bug mode", @"\g"));
        this.CommandList.Add(new ConfigItem("Switch to Hell sending", @"\h"));
        this.CommandList.Add(new ConfigItem("Transmit enable/disable", @"\i"));
        this.CommandList.Add(new ConfigItem("Dah to dit ratio (300 = 3.00) ###", @"\j###", true));
        this.CommandList.Add(new ConfigItem("CW Training Module", @"\k"));
        this.CommandList.Add(new ConfigItem("Set weighting (50 = normal) ##", @"\l##", true));
        this.CommandList.Add(new ConfigItem("Set Farnsworth speed ###", @"\m###", true));
        this.CommandList.Add(new ConfigItem("Toggle paddle reverse", @"\n"));
        this.CommandList.Add(new ConfigItem("Toggle sidetone on/off", @"\o"));
        this.CommandList.Add(new ConfigItem("Program memory #", @"\p#", true));
        this.CommandList.Add(new ConfigItem("Switch to QRSS mode, dit length ## seconds", @"\q##", true));
        this.CommandList.Add(new ConfigItem("Switch to regular speed mode", @"\r"));
        this.CommandList.Add(new ConfigItem("Status", @"\s"));
        this.CommandList.Add(new ConfigItem("Tune mode", @"\t"));
        this.CommandList.Add(new ConfigItem("Toggle potentiometer active / inactive", @"\v"));
        this.CommandList.Add(new ConfigItem("Set speed in WPM ###", @"\w###", true));
        this.CommandList.Add(new ConfigItem("Switch to transmitter #", @"\x#", true));
        this.CommandList.Add(new ConfigItem("Change wordspace to # elements (# = 1 to 9)", @"\y#", true));
        this.CommandList.Add(new ConfigItem("Autospace on/off", @"\z"));
        this.CommandList.Add(new ConfigItem("Create prosign", @"\+"));
        this.CommandList.Add(new ConfigItem("Repeat play memory ##", @"\!##", true));
        this.CommandList.Add(new ConfigItem("Set memory repeat (milliseconds)", @"\|####", true));
        this.CommandList.Add(new ConfigItem("Toggle paddle echo", @"\*"));
        this.CommandList.Add(new ConfigItem("Toggle wait for carriage return to send CW / send CW immediately", @"\^"));
        this.CommandList.Add(new ConfigItem("Toggle CMOS Super Keyer Timing on/off", @"\&"));
        this.CommandList.Add(new ConfigItem("Set CMOS Super Keyer Timing ##%", @"\%##"));
        this.CommandList.Add(new ConfigItem("Toggle dit buffer on/off", @"\."));
        this.CommandList.Add(new ConfigItem("Toggle dah buffer on/off", @"\-"));
        this.CommandList.Add(new ConfigItem("CW send echo inhibit toggle", @"\:"));
        this.CommandList.Add(new ConfigItem("QLF mode on/off", @"\{"));
        this.CommandList.Add(new ConfigItem("Send serial number, then increment", @"\>"));
        this.CommandList.Add(new ConfigItem("Send current serial number", @"\<"));
        this.CommandList.Add(new ConfigItem("Send current serial number in cut numbers", @"\("));
        this.CommandList.Add(new ConfigItem("Send serial number with cut numbers, then increment", @"\)"));
        this.CommandList.Add(new ConfigItem("Set Quiet Paddle Interruption", @"\["));
        this.CommandList.Add(new ConfigItem("Toggle American Morse mode", @"\="));
        this.CommandList.Add(new ConfigItem("Mill Mode", @"\@"));
        this.CommandList.Add(new ConfigItem("Set potentiometer range - low ## / high ##", @"\}####", true));
       // this.CommandList.Add(new ConfigItem(" Hold PTT active with buffered characters", @"\\""));
        this.CommandList.Add(new ConfigItem("PTT Enable / Disable", @"\]"));
        this.CommandList.Add(new ConfigItem("Immediately clear the buffer, stop memory sending, etc.", @"\\"));

    }
    #endregion
}
