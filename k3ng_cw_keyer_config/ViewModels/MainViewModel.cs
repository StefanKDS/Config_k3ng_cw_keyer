using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Windows.Input;

namespace k3ng_cw_keyer_config.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region Fields
    private string _selectedPort = "";
    private readonly SerialPort _serialPort;
    #endregion

    #region Konstruktor / Destruktor
    public MainViewModel()
    {
        this.PortList = new ObservableCollection<string>();
        this._serialPort = new SerialPort();

        this.ClearOutputCommand = new RelayCommand(this.OnClearOutput);
        this.SendCommand = new RelayCommand(this.OnSend);

        this.ListAlAvailablePorts();
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

    public string OutputText { get; set; }

    public string InputText { get; set; }

    public bool IsAutoScrollChecked { get; set; }

    public int OutputCaretIndex { get; set; }

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

            }
        }
    }

    #endregion

    #region Commands
    public ICommand ClearOutputCommand { get; }

    public ICommand SendCommand { get; }

    private void OnClearOutput()
    {
        this.OutputText = string.Empty;
    }

    private void OnSend()
    {
        this._serialPort.Write(this.InputText);
    }
    #endregion

    #region Helper
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
        this._serialPort.Handshake = Handshake.XOnXOff;
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
    }

    #endregion
}
