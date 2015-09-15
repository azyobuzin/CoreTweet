if([Environment]::Is64BitOperatingSystem)
{
    $msbuild = "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
}
else
{
    $msbuild = "C:\Program Files\MSBuild\14.0\Bin\MSBuild.exe"
}

& $msbuild RestApisGen\RestApisGen.csproj /p:Configuration=Debug
RestApisGen\bin\RestApisGen.exe
