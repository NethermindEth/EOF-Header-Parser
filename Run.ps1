echo "Building ... "
Start-Process -FilePath "dotnet" -ArgumentList "build", "-c", "Release" -Wait  -NoNewWindow
echo "Running with inputs from Holiman corpus"
Start-Process -FilePath "dotnet" -ArgumentList "run", "--no-build", "-c", "Release", "--Inputs", "https://raw.githubusercontent.com/holiman/txparse/main/eofparse/all.input" -Wait -RedirectStandardOutput "all.output" -RedirectStandardError "all.err"  -NoNewWindow
echo "Diffing result against Holiman corpus"
Start-Process -FilePath "dotnet" -ArgumentList "run", "--no-build", "-c", "Release", "--DiffFiles", "all.output", "https://raw.githubusercontent.com/holiman/txparse/main/eofparse/all.output" -Wait -NoNewWindow
