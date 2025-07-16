name "Selezione if else"
; programma che se il primo numero e' maggiore  oppure uguale al secondo effettua 
; la sottrazzione altrimenti li addizziona. Il risultato in entrambi i casi va salvato nella variabile Ris 

data segment
    ; add your data here! 
    Num1 DW 3
    Num2 DW 5
    Ris  DW 0000h
    
    Stringa DB "press any key...$"
ends

stack segment
    dw   128  dup(0)
ends

code segment
start:
; set segment registers:
    MOV AX, data
    MOV DS, AX
    
    MOV AX, Num1
    MOV BX, Num2    
    CMP AX,BX ;confrontare i due registri attraverso una sottrazione 
    JGE salta
    ADD AX,BX
    MOV Ris, AX 
    JMP fine:   
    salta:
    SUB AX,BX
    MOV Ris, AX
     fine: 
    ; interrupt per scrivere a video una stringa
            
    LEA DX, Stringa
    MOV AH, 9h
    INT 21h        ; output string at ds:dx
    
    ; interrupt per leggere un carattere da tastiera con echo    
    MOV AH, 1h
    INT 21h
    
    ;interrupt per uscire dal programma e restituire controllo al sistema operativo
    MOV AH, 4Ch 
    INT 21h    
ends

end start ; set entry point and stop the assembler.