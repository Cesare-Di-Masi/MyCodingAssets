; multi-segment executable file template.

data segment
    ; add your data here!
    Vet1 DW 3 DUP (0)
    Msg1 DB "Inserisci all'interno del primo vettore un numero a 16 bit: $"
    Vet2 DW 3 DUP (0) 
    Msg2 DB "Inserisci all'interno del secondo vettore un numero a 16 bit: $"
    Diversi DB "I due vettori sono diversi $"
    Uguali DB "I due vettori sono uguali $"
    pkey DB "press any key...$"
ends

stack segment
    DW   128  DUP(0)
ends

code segment
start:
; set segment registers: 
    MOV AX, data
    MOV DS, AX
     
    ; add your code here 
    ;Leggi il primo vettore
    MOV SI, Vet1   
    MOV CX, 3
    
Loop_vet1:  

    LEA DX, Msg1
    MOV AH, 9
    INT 21h
    MOV AH, 01h
    INT 21h
    SUB AL, 30h    ;Converti
    MOV [SI], AX
    ADD SI, 2
    LOOP Loop_vet1
    
    ;Leggo il secondo vettore 
    LEA SI, Vet2
    MOV CX, 3 
    
Loop_vet2: 

    LEA DX, Msg2 
    MOV AH, 9
    INT 21h
    MOV AH, 01h
    INT 21h
    SUB AL, 30h
    MOV [SI], 2
    ADD SI, 2
    LOOP Loop_vet2
    
    ;Confronto i 2 vettori    
    LEA SI, Vet1
    LEA DI, Vet2
    MOV CX, 3
    
If:
    MOV AX, [SI]
    MOV BX, [DI]
    CMP AX, BX
    JNE Vet_diversi
    ADD SI, 2
    ADD DI, 2
    LOOP if
    
    LEA DX, Uguali
    MOV AH, 09h
    INT 21h
    JMP Fine 
    
Vet_diversi:
    LEA DX, Diversi
    MOV AH, 09h
    INT 21h



Fine:
    MOV AH, 09h
    INT 21h            
ends

end start ; set entry point and stop the assembler.
