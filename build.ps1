Remove-Module assemblyInfo -ErrorAction SilentlyContinue
Import-Module assemblyInfo

Remove-Module xunit -ErrorAction SilentlyContinue
Import-Module xunit

properties {
	
	$build = @{}
	$build.Configuration = "Release"
	$build.Number = if($env:BUILD_NUMBER) { $env:BUILD_NUMBER } else { "0" }
	$build.Directory = Resolve-Path .

	$vcsRoot = @{}
	$vcsRoot.Directory = (Split-Path -Parent $build.Directory)
	
	$artifacts = @{}
	$artifacts.Directory = Join-Path $vcsRoot.Directory "Artifacts" 
		
	$solution = @{}
	$solution.Directory = Join-Path $vcsRoot.Directory "Source" 
	$solution.PackagesDirectory = Join-Path $solution.Directory "Packages" 
	$solution.File =  @(Get-ChildItem $solution.Directory  -Filter *.sln)[0].FullName
	
	$nuget = @{}
	$nuget.Directory = Join-Path $solution.Directory ".nuget" 
	$nuget.File = Join-Path $nuget.Directory "NuGet.exe" 
		
}

Task Run-CI-Build -Depends System-Information, Clean, Restore-Packages, Compile, Run-Tests

Task System-Information {

	Write-Host  "Powershell version $($PSVersionTable.PSVersion)"
	Write-Host  "CLR version $($PSVersionTable.CLRVersion)"

}

Task Clean {
	
	Write-Host "Cleaning artifacts directory"
	Remove-Item $artifacts.Directory -Force -Recurse -ErrorAction SilentlyContinue

	Write-Host "Cleaning nuget packages directory"
	Remove-Item $solution.PackagesDirectory -Force -Recurse -ErrorAction SilentlyContinue

}

Task Restore-Packages {

	Write-Host "Restoring nuget packages"
 	&$nuget.File restore $solution.File
}

Task Compile -Depends Clean, Version-AssemblyInfo {

Write-Host "Compiling solution"
	exec { msbuild $($solution.File) /t:Clean /t:Build  /p:Configuration=$($build.configuration) /p:RunOctoPack=true /p:OctoPackPublishPackageToFileShare=$($artifacts.Directory)  /v:q  /clp:ErrorsOnly /nologo }

}

Task Run-Tests {
	Get-ChildItem $solution.Directory -Recurse -Include *.unittests,*.integrationTests |
    ?{ ($_ -is [System.IO.DirectoryInfo]) -and (Test-Path "$($_.FullName)\bin\$($build.configuration)\$($_.Name).dll") } |
    %{
			$base = $_.FullName
			$name = $_.Name
			$testAssemblyFile = Join-Path $base (Join-Path "bin" (Join-Path $build.configuration "$name.dll"))
			Invoke-Xunit $testAssemblyFile 
		}
}

Task Version-AssemblyInfo {

	Write-Output "Writing build number [$($build.number)] to assemblyinfo"		  
	
	$assemblyFile = Join-Path $($solution.Directory) GlobalAssemblyInfo.cs
	$buildNumber = $build.Number
	
	if (Test-Path($assemblyFile))
	{
		$build.Version = Update-AssemblyBuildNumber -assemblyFile $assemblyFile -buildNumber $buildNumber
	}
		
	
}



<# 
 Exits the script when an error occurs and prints a message to the user
#> 
function Exit-Build
{
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, Mandatory = $true)][String]$Message
    )
 
    Write-Host $("`nExiting build because task [{0}] failed.`n->`t$Message.`n" -f $psake.context.Peek().currentTaskName) -ForegroundColor Red
 
    Exit
}






