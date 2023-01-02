# EOF-Header-Parser
a simple parser of EOF headers from Shanghai compatible bytecodes 

# Usage : 
* without cmd arguments : Assumes Full EOF
* with cmd arguments : ``--deactivate|-d <Eip-Number>+``, Every EIP deactivated will deactivate the ones that depend on it

# Example : 
* Input 

``cat Tests.input > dotnet run`` or ``cat Tests.input > dotnet run -d 3670``

* Output 
```
err: CodeSection contains undefined opcode CALLCODE
OK 60006001f3
```
