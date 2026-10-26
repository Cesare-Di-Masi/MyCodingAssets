; multi-segment executable file template.
; Esercizio 2 somma di 10+20 salvati un due variabili Num1 e Num2 e risultato salvato nelle variabili Ris
data segment
    ; add your data here!
    Num1 DW 40h
    Num2 DW 20h
    Ris  DW  0h
    pkey db "press any key...$"
ends

stack segment
    dw   128  dup(0)
ends

code segment
start:
MOV AX,data
MOV DS,AX
MOV AX,Num1
MOV BX,Num2

CMP AX,BX

JGE sottrazione
ADD AX,BX
MOV RIS,AX   
jmp skip

sottrazione:
SUB AX,BX
MOV Ris,AX

skip:

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
