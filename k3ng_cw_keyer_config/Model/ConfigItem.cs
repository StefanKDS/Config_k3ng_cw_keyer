namespace k3ng_cw_keyer_config.Model;
public class ConfigItem
{
    #region Konstruktor
    public ConfigItem()
    {
    }

    public ConfigItem(string commandName, string command, bool hasParameter = false)
    {
        this.Command = command;
        this.CommandName = commandName;
        this.HasParameter = hasParameter;
    }
    #endregion

    #region Properties
    public string Command { get; set; }

    public string CommandName { get; set; }

    public bool HasParameter { get; set; }
    #endregion
}
