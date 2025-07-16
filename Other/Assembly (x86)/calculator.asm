; multi-segment executable file template.

data segment
    
    pkey db "premere un tasto fra 1 e 4 (sum, sub, mul, div)"
    Num1 DW 10h
    Num2 DW 10h
    Ris DW 0h
    Op DW 4
    
ends

stack segment
    dw   128  dup(0)
ends

code segment
start:
; set segment registers:
    mov ax, data
    mov ds, ax
    
    Mov AX,Num1
    Mov BX,Num2
    
    cmp Op,1
    JE somma
    cmp Op,2h
    JE sottrazione
    cmp Op,3
    JE moltiplicazione
    cmp Op,4
    JE divisione 
    Jmp salta:
    
    
    somma:
    ADD AX,BX
    Jmp salta:
    
    sottrazione:
    SUB AX,BX
    Jmp salta:
    
    moltiplicazione: 
    MUL BX
    Jmp salta:
    
    divisione:
    DIV BX
    Jmp salta:  
    
    salta:
    MOV Ris,AX
            
    
ends

end start ; set entry point and stop the assembler.
