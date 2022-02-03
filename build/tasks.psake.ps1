# SYNOPSIS: This is a psake task file.
FormatTaskName "$([string]::Concat([System.Linq.Enumerable]::Repeat('-', 70)))`r`n  {0}`r`n$([string]::Concat([System.Linq.Enumerable]::Repeat('-', 70)))";

Properties {
	$Dependencies = @(@{"Name"="Ncrement";"Version"="8.2.18"});

    # Arguments
	$Major = $false;
	$Minor = $false;
	$Filter = $null;
	$InPreview = $false;
	$Interactive = $true;
	$InProduction = $false;
	$Configuration = "Debug";
	$EnvironmentName = $null;
	$SqlDialect = "SQLServer";

	# Files & Folders
	$MSBuildExe = "";
	$ToolsFolder = "";
	$SecretsFilePath = "";
	$SolutionFolder = (Split-Path $PSScriptRoot -Parent);
	$SolutionName =   (Split-Path $SolutionFolder -Leaf);
	$ArtifactsFolder = (Join-Path $SolutionFolder "artifacts");
	$ManifestFilePath = (Join-Path $PSScriptRoot  "manifest.json");
	$MigrationFolder = (Join-Path $SolutionFolder "src/*.Migration/tsql" | Resolve-Path)
}

Task "Default" -depends @("compile", "test", "pack");

Task "Publish" -depends @("clean", "version", "compile", "test", "pack", "push-nuget") `
-description "This task compiles, test then publish all packages to their respective destination.";

# ======================================================================

Task "Restore-Dependencies" -alias "restore" -description "This task generate and/or import all file and module dependencies." `
-action {
	# Import powershell module dependencies
	# ==================================================
	foreach ($module in $Dependencies)
	{
		$modulePath = Join-Path $ToolsFolder "$($module.Name)/*/*.psd1";
		if (-not (Test-Path $modulePath)) { Save-Module $module.Name -MaximumVersion $module.Version -Path $ToolsFolder; }
		Import-Module $modulePath -Force;
		Write-Host "  * imported the '$($module.Name)-$(Split-Path (Get-Item $modulePath).DirectoryName -Leaf)' powershell module.";
	}

	# Generating the build manifest file
	# ==================================================
	if (-not (Test-Path $ManifestFilePath))
	{
		New-NcrementManifest | ConvertTo-Json | Out-File $ManifestFilePath -Encoding utf8;
		Write-Host "  * added 'build/$(Split-Path $ManifestFilePath -Leaf)' to the solution.";
	}
}

#region ----- PUBLISHING -----------------------------------------------

Task "Package-Solution" -alias "pack" -description "This task generates all deployment packages." `
-depends @("restore") -action {
	if (Test-Path $ArtifactsFolder) { Remove-Item $ArtifactsFolder -Recurse -Force; }
	New-Item $ArtifactsFolder -ItemType Directory | Out-Null;
	
	$project = Join-Path $SolutionFolder "src/$SolutionName/*.*proj" | Get-Item;
	Write-Separator "dotnet pack $($project.BaseName)";
	Exec { &dotnet pack $project.FullName --output $ArtifactsFolder --configuration $Configuration -p:EnvironmentName=$EnvironmentName; }

	Join-Path $SolutionFolder "src/*.VSIX/bin/$Configuration/*.vsix" | Get-Item | Copy-Item -Destination $ArtifactsFolder;
}

Task "Publish-NuGet-Packages" -alias "push-nuget" -description "This task publish all nuget packages to a nuget repository." `
-precondition { return ($InProduction -or $InPreview ) -and (Test-Path $ArtifactsFolder -PathType Container) } `
-action {
    foreach ($nupkg in Get-ChildItem $ArtifactsFolder -Filter "*.nupkg")
    {
        Write-Separator "dotnet nuget push '$($nupkg.Name)'";
        Exec { &dotnet nuget push $nupkg.FullName --source "https://api.nuget.org/v3/index.json"; }
    }
}

Task "Publish-VSIX-Package" -alias "push-vsix" -description "This task publish all VSIX packages to https://marketplace.visualstudio.com/" `
-precondition { return ($InProduction -or $InPreview ) -and (Test-Path $ArtifactsFolder -PathType Container) } `
-action {
	[string]$vsixPublisher = Join-Path "$($env:ProgramFiles)*" "Microsoft Visual Studio\*\*\VSSDK\VisualStudioIntegration\Tools\Bin\VsixPublisher.exe" | Resolve-Path -ErrorAction Stop;
	$package = Join-Path $ArtifactsFolder "*.vsix" | Get-Item;
	$manifest = Join-Path $PSScriptRoot "visual-studio-maketplace-manifest.json" | Get-Item;
	$pat = Get-Secret "vsixMarketplace" "VISUAL_STUDIO_MARKETPLACE_PAT";
	
	Write-Separator "VsixPublish publish -payload '$($package.Name)'";
	#Exec { &$vsixPublisher login -publisherName "Tekcari" -personalAccessToken $pat; }
	#Exec { &$vsixPublisher publish -payload $package.FullName -publishManifest $manifest.FullName -personalAccessToken $pat -ignoreWarnings "VSIXValidatorWarning01,VSIXValidatorWarning02"; }
}

Task "Add-GitReleaseTag" -alias "tag" -description "This task tags the lastest commit with the version number." `
-precondition { return ($InProduction -or $InPreview ) } `
-depends @("restore") -action {
	$version = $ManifestFilePath | Select-NcrementVersionNumber $EnvironmentName -Format "C";

	if (-not ((&git status | Out-String) -match 'nothing to commit'))
	{
		Exec { &git add .; }
		Write-Separator "git commit";
		Exec { &git commit -m "Increment version number to '$version'."; }
	}

	Write-Separator "git tag '$version'";
	Exec { &git tag --annotate "v$version" --message "Version $version"; }
}

#endregion

#region ----- DATABASE MIGRATION -----------------------------------------------

Task "Add-MigratonScript" -alias "new-script" -description "This task generate a new migration script." `
-depends @("restore", "version", "compile") -action {
	$version = $ManifestFilePath | Select-NcrementVersionNumber $EnvironmentName -Format "C";
	$emptySchemaContent = '<?xml version="1.0" encoding="utf-8"?><schema xmlns="https://raw.githubusercontent.com/Ackara/Daterpillar/master/src/Daterpillar/daterpillar.xsd"></schema>';

	[string]$localSchemaPath  = Join-Path $MigrationFolder "local.schema.xml";
	if (-not (Test-Path $localSchemaPath)) { $emptySchemaContent | Out-File $localSchemaPath -Encoding utf8;  }

	[string]$remoteSchemaPath = Join-Path $MigrationFolder "server.schema.xml";
	if (-not (Test-Path $remoteSchemaPath)) { $emptySchemaContent | Out-File $remoteSchemaPath -Encoding utf8; }

	$description = Read-Host "enter a description";
	if ([string]::IsNullOrEmpty($description)) { $description = "update_schema"; } else { $description = $description.Replace(' ', '_'); }
	$scriptPath = Join-Path $MigrationFolder "V$($version)__$description.sql";

	[string]$dll = Join-Path $SolutionFolder "src/*.Migration/bin/$Configuration/*/*.Migration.dll" | Resolve-Path;
	Exec { &dotnet $dll $localSchemaPath $remoteSchemaPath $scriptPath; }
}

Task "Update-Database" -alias "push-db" -description "This task apply pending sql changes to the designated database." `
-depends @("restore") -action {
	# === Content Database ===

	[string]$localSchemaPath  = Join-Path $MigrationFolder "local.schema.xml" | Resolve-Path;
	[string]$remoteSchemaPath = Join-Path $MigrationFolder "server.schema.xml" | Resolve-Path;

	Write-Separator "evolve migrate";

	$connectionString = Get-Secret "$($EnvironmentName).database.main" "DATABASE_CONNECTION_STRING";
	Exec { &dotnet evolve migrate $SqlDialect --connection-string $connectionString --location $MigrationFolder; }
	Copy-Item $localSchemaPath $remoteSchemaPath -Force;
	Exec { &dotnet evolve info $SqlDialect --connection-string $connectionString --location $MigrationFolder; }

	# === Security Database ===

	[string]$project = Join-Path $SolutionFolder "src\*.ASP" | Resolve-Path;

	Push-Location $project;
	Write-Separator "dotnet ef database update";
	try { Exec { &dotnet ef database update; } } finally { Pop-Location; }
}

Task "Clear-Database" -alias "clean-db" -description "This task erase all tabled from the local database instance." `
-depends @() -action {
	[string]$localSchemaPath  = Join-Path $MigrationFolder "local.schema.xml" | Resolve-Path;
	[string]$remoteSchemaPath = Join-Path $MigrationFolder "server.schema.xml" | Resolve-Path;

	Write-Separator "evolve migrate";

	$connectionString = Get-Secret "local.database.main" "DATABASE_CONNECTION_STRING";
	Exec { &dotnet evolve erase $SqlDialect --connection-string $connectionString --location $MigrationFolder; }
	Exec { &dotnet evolve info $SqlDialect --connection-string $connectionString --location $MigrationFolder; }

	Get-ChildItem $MigrationFolder -Filter "*.sql" | Remove-Item;
	'<schema xmlns="https://raw.githubusercontent.com/Ackara/Daterpillar/master/src/Daterpillar/daterpillar.xsd"></schema>' | Out-File -FilePath $remoteSchemaPath -Encoding utf8;
}

Task "Truncate-Database" -alias "truncate-db" -description "This task truncate all tabled data from the local database instance." `
-depends @() -action {

	$connectionString = Get-Secret "local.connection.main" "DATABASE_CONNECTION_STRING";
	Write-Separator "evolve erase";
	Exec { &dotnet evolve erase $SqlDialect --connection-string $connectionString --location $MigrationFolder; }
	Write-Separator "evolve migrate";
	Exec { &dotnet evolve migrate $SqlDialect --connection-string $connectionString --location $MigrationFolder; }
	Exec { &dotnet evolve info $SqlDialect --connection-string $connectionString --location $MigrationFolder; }
}

#endregion

#region ----- COMPILATION ----------------------------------------------

Task "Clean" -description "This task removes all generated files and folders from the solution." `
-action {
	foreach ($itemsToRemove in @("artifacts", "TestResults", "*/*/bin/", "*/*/obj/", "*/*/node_modules/", "*/*/package-lock.json"))
	{
		$itemPath = Join-Path $SolutionFolder $itemsToRemove;
		if (Test-Path $itemPath)
		{
			Resolve-Path $itemPath `
				| Write-Value "  * removed '{0}'." -PassThru `
					| Remove-Item -Recurse -Force;
		}
	}
}

Task "Increment-Version-Number" -alias "version" -description "This task increments all of the projects version number." `
-depends @("restore") -action {
	$manifest = $ManifestFilePath | Step-NcrementVersionNumber -Major:$Major -Minor:$Minor -Patch | Edit-NcrementManifest $ManifestFilePath;
	$newVersion = $ManifestFilePath | Select-NcrementVersionNumber $EnvironmentName;

	foreach ($item in @("*/*/*.*proj", "src/*/*.vsixmanifest"))
	{
		$itemPath = Join-Path $SolutionFolder $item;
		if (Test-Path $itemPath)
		{
			Get-ChildItem $itemPath | Update-NcrementProjectFile $ManifestFilePath `
				| Write-Value "  * incremented '{0}' version number to '$newVersion'.";
		}
	}
}

Task "Build-Solution" -alias "compile" -description "This task compiles projects in the solution." `
-action {
	$solutionFile = Join-Path $SolutionFolder "*.sln" | Get-Item;
	Write-Separator "msbuild '$($solutionFile.Name)'";
	Exec { &$MSBuildExe $solutionFile.FullName -property:"Configuration=$Configuration;EnvironmentName=$EnvironmentName" -restore ; }
	#Exec { &dotnet build $solutionFile.FullName --configuration $Configuration -p:"EnvironmentName=$EnvironmentName"; }
}

Task "Run-Tests" -alias "test" -description "This task invoke all tests within the 'tests' folder." `
-action {
	foreach ($item in @("tests/*MSTest/*.*proj"))
	{
		[string]$projectPath = Join-Path $SolutionFolder $item;
		if (Test-Path $projectPath -PathType Leaf)
		{
			$projectPath = Resolve-Path $projectPath;
			Write-Separator "dotnet test '$(Split-Path $projectPath -Leaf)'";
			Exec { &dotnet test $projectPath --configuration $Configuration; }
		}
	}
}

#endregion

#region ----- FUNCTIONS ------------------------------------------------

function Write-Value
{
	Param(
		[Parameter(Mandatory)]
		[string]$FormatString,

		$Arg1, $Arg2,

		[Alias('c', "fg")]
		[System.ConsoleColor]$ForegroundColor = [System.ConsoleColor]::Gray,

		[Parameter(ValueFromPipeline)]
		$InputObject,

		[switch]$PassThru
	)

	PROCESS
	{
		Write-Host ([string]::Format($FormatString, $InputObject, $Arg1, $Arg2)) -ForegroundColor $ForegroundColor;
		if ($PassThru -and $InputObject) { return $InputObject }
	}
}

function Write-Separator([string]$Title = "", [int]$length = 70)
{
	$header = [string]::Concat([System.Linq.Enumerable]::Repeat('-', $length));
	if (-not [String]::IsNullOrEmpty($Title))
	{
		$header = $header.Insert(4, " $Title ");
		if ($header.Length -gt $length) { $header = $header.Substring(0, $length); }
	}
	Write-Host "`r`n$header`r`n" -ForegroundColor DarkGray;
}

function Get-Secret
{
	Param(
		[Parameter(Mandatory)]
		[string]$JPath,

		[Parameter(Mandatory)]
		[string]$EnvironmentVariable
	)

	$result = [Environment]::ExpandEnvironmentVariables("%$EnvironmentVariable%");
	if ([string]::IsNullOrEmpty($result) -or ($result -eq "%$EnvironmentVariable%"))
	{
		$result = Get-Content $SecretsFilePath | Out-String | ConvertFrom-Json;
		$properties = $JPath.Split(@('.', '/', ':'));
		foreach($prop in $properties)
		{
			$result = $result.$prop;
		}
	}
	return $result;
}


function Open-WebLink([string]$publishSettings)
{
	[xml]$xml = Get-Content $publishSettings;
	$url = $xml.publishData.publishProfile.destinationAppUrl;
	Start-Process $url;
}

function Get-Alt([string]$value, [string]$default = ""){
	if ([string]::IsNullOrWhiteSpace($value)) { return $default; } else { return $value; }
}

#endregion