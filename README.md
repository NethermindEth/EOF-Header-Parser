# EOF-Header-Parser
a simple parser of EOF headers from Shanghai compatible bytecodes 

# Run : 
* ``dotnet run <arguments>+``

# Usage : 
* without cmd arguments : Assumes Full EOF
* with cmd arguments : 
  * Empty, takes a line from stdin and validates it against Full-Eof and gives result to stout
  * ``--deactivate|-d <Eip-Number>+``, Every EIP deactivated will deactivate the ones that depend on it
  * ``--diff <filepaths>+``, diffs results of different files
# Example : 
* Input 

``cat Tests.input > dotnet run`` or ``cat Tests.input > dotnet run -d 3670``

* Output 
```
err: CodeSection contains undefined opcode CALLCODE
OK 60006001f3
```
