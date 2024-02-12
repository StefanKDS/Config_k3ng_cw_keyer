using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using System.IO.Ports;
using System;
using System.Linq;

namespace k3ng_cw_keyer_config.Model
{
    public class FavItem
    {
        #region Fields
        private readonly SerialPort _serialPort;
        private Action<string> _sendMethod;
        #endregion

        #region Construktor
        public FavItem()
        {
        }

        public FavItem(Action<string> sendMethod, ConfigItem configItem, string parameter)
        {
            this.ConfigItem = configItem;
            this.Parameter = parameter;
            this._sendMethod = sendMethod;

            this.SendFavCommand = new RelayCommand(this.OnSendFav);
        }
        #endregion

        #region Properties
        public ConfigItem ConfigItem { get; set; }

        public string Parameter { get; set; }

        public ICommand SendFavCommand { get; }
        #endregion

        #region Helper
        private static int ExtractNbrOfDigits(string parameter)
        {
            return parameter.Count(c => c == '#');
        }

        private void OnSendFav()
        {
            int nbrOfDigits = ExtractNbrOfDigits(this.ConfigItem.Command);
            string cmd = this.ConfigItem.Command.Remove(this.ConfigItem.Command.Length - nbrOfDigits, nbrOfDigits);
            cmd += this.Parameter;
            this._sendMethod(cmd);
        }
        #endregion
    }
}
