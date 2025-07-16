; multi-segment executable file template.

data segment 
    
    Num1 DW 5
    Num2 Dw 3
    Ris  DW 0000h 
    ; add your data here!
    pkey db "press any key...$"
ends

stack segment
    dw   128  dup(0)
ends

code segment
start:
; set segment registers:
    mov ax, data
    mov ds, ax
    
    MOV AX, Ris
    MOV DX, Num1
    MOV BX, Num2 
InizioCiclo:
    CMP BX,0
    JE FineCiclo 
    ADD AX,DX
    DEC BX
    JMP InizioCiclo  
FineCiclo:
     MOV Ris,AX
    

    ; add your code here
            
    lea dx, pkey
    mov ah, 9
    int 21h        ; output string at ds:dx
    
    ; wait for any key....    
    mov ah, 1
    int 21h
    
    mov ax, 4c00h ; exit to operating system.
    int 21h    
ends

end start ; set entry point and stop the assembler.
