using System.ComponentModel;

namespace Nexus.SetupWizard;

public class SetupConfiguration : INotifyPropertyChanged
{
    public string InstallPath { get; set; } = @"C:\Program Files\SOCAR\NexusPM";
    public string DataPath { get; set; } = @"D:\NexusPM\Data";
    public string DocumentsPath { get; set; } = @"D:\NexusPM\Documents";
    
    public SqlServerOption SqlOption { get; set; } = SqlServerOption.UseExisting;
    public string SqlInstanceName { get; set; } = "NEXUSPM";
    public SqlServerEdition SqlEdition { get; set; } = SqlServerEdition.Express;
    public string SqlAdminPassword { get; set; } = "";
    
    public AuthMode AuthMode { get; set; } = AuthMode.ActiveDirectory;
    public string DomainName { get; set; } = "";
    public string AdUserGroup { get; set; } = "NexusPM_Users";
    public string AdAdminGroup { get; set; } = "NexusPM_Admins";
    
    public event PropertyChangedEventHandler? PropertyChanged;
}

public enum SqlServerOption { UseExisting, InstallNew }
public enum SqlServerEdition { Express, Developer, Standard, Enterprise }
public enum AuthMode { ActiveDirectory, LocalUsers, Mixed }
