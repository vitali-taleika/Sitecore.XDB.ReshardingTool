$prefix = "test2"
$configPath = "C:\Sitecore 9.1.0\CreateXDB.json"
$ToolFolderPath = "C:\inetpub\wwwroot\test.xconnect\App_Data\collectiondeployment"
$SqlServer = ".\SQLENTERPRISE"
$SqlAdminUser = "sa"
$SqlAdminPassword = "Password1!"

$XDBParams = @{
 Path = $configPath
 ToolFolder = $ToolFolderPath
 SqlDbPrefix = $prefix
 SqlServer = $SqlServer
 SqlAdminUser = $SqlAdminUser
 SqlAdminPassword = $SqlAdminPassword
}

Install-SitecoreConfiguration @XDBParams -Verbose
#UnInstall-SitecoreConfiguration @XDBParams -Verbose