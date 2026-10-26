; multi-segment executable file template.

data segment
    ; add your data here!
    Num1 dw 10h
    Num2 dw 10h
    Counter dw 0h        
    Ris dw 0000h
    Stringa db "press any key...$"
ends

stack segment
    dw   128  dup(0)
ends

code segment
start:
; set segment registers:
    mov ax, data
    mov ds, ax
    
    MOV AX,Num1
    MOV BX,Num2
    
    
    
    
    ciclo:
    CMP Counter, AX
    JE esci:
    ADD Ris,BX
    INC Counter
    jmp ciclo:
    
    
    esci:
   
    
    ; interrupt per scrivere a video una stringa
            
    lea dx, Stringa
    mov ah, 9h
    int 21h
    
    ; interrupt per leggere un carattere da tastiera con echo (ti fa vedere cosa scrivi)   
    mov ah, 1h
    int 21h
                 
    ; interrupt per uscire dal programma e restituire controllo al sistema operativo             
    mov ah, 4Ch  
    int 21h      
    
ends

end start ; set entry point and stop the assembler.
