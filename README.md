# EOF-Header-Parser
a simple parser of EOF headers from Shanghai compatible bytecodes 

# Run : 
* ``dotnet run <arguments>*``

# Usage : 
* without cmd arguments : Assumes Full EOF Reads from stdin
* with cmd arguments : 
  * Empty, takes a line from stdin and validates it against Full-Eof and gives result to stout
  * ``--Deactivate <Eip-Number>+``, Eip-Number must be an integer and must be a valid EIP number. EIP deactivates all EIPs that depend on it.
  * ``--DiffFiles <filepaths>+``, diffs results of different files
  * ``--Bytecode <Bytecode>+``, Bytecode must be a valid bytecode (1 contiguous hexadecimal bytearray).
  * ``--Inputs <Input>+``, Input must be a valid file path or a file Uri.
# Example : 
* Input 

``cat Tests.input > dotnet run`` or ``cat Tests.input > dotnet run -d 3670``

* Output 
```
err: CodeSection contains undefined opcode CALLCODE
OK 60006001f3
```
