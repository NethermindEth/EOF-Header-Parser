echo "Building ... "
dotnet build -c Release 
echo "Running with inputs from Holiman corpus"
dotnet run --no-build --Inputs "https://raw.githubusercontent.com/holiman/txparse/main/eofparse/all.input" 1> all.output 2> all.err
echo "Diffing result against Holiman corpus"
dotnet run --no-build --DiffFiles all.output "https://raw.githubusercontent.com/holiman/txparse/main/eofparse/all.output"