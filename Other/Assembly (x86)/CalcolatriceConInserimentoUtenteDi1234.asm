; multi-segment executable file template.

data segment
    ; add your data here!
    pkey db "press any key...$"
    Num1 DW 4
    Num2 DW 2
    Ris  DW 0
    Op   DB 0
ends

stack segment
    dw   128  dup(0)
ends

code segment
start:
mov AX,Num1
mov BX,Num2
mov DX,Ris
mov AH,1h
int 21h
sub AL,30
mov AL,Op

cmp Op,1 





   
ends

end start ; set entry point and stop the assembler.
