; multi-segment executable file template.

data segment
    Num1        DB 5h
    MsgNum1     DW "Inserisci il primo numero: $"
    Num2        DB 10h 
    MsgNum2     DW "Inserisci il secondo numero: $"
    Op          DB 1
    Inserimento DB "Inserisci il tipo di operazione 1-4: $"
    Errore      DB "Hai inserito il valore sbagliato, riprova $"
    Ris         DW 0h
       
    
    string DB "press any key...$"
ends

stack segment
    DW   128  dup(0)
    Top DW ?
ends

code segment 
    
Start:   

    ;Data segment
    MOV AX, data
    MOV DS, AX
    MOV ES, AX  
    
    MOV AX, stack 
    MOV SS, AX
    LEA SP, Top
    
    ;PUSH MsgNum1 e PUSH MsgNum2 non posso farlo
    LEA DX, MsgNum1
    MOV AH, 9
    INT 21h  
    MOV AH, 01h
    INT 21h
    sub al
    CALL NewLine
    LEA DX, MsgNum2
    MOV AH, 9
    INT 21h  
    MOV AH, 01h
    INT 21h  
    CALL NewLine
    
    ;Chiedi all'utente di inserire un numero
    MOV AH, 9
    LEA DX, Inserimento 
    INT 21h
    
    ;Leggi il numero inserito dall'utente
    MOV AH, 01h 
    INT 21h     
    SUB AL, 30h
    MOV Op, AL 
    
    CALL NewLine
    
    ;Verifica l'operazione da eseguire
    CMP Op, 1  
    JE Somma 
    
    CMP Op, 2 
    JE Sottrazione 
    
    CMP Op, 3
    JE Moltiplicazione 
    
    CMP Op, 4 
    JE Divisione  
    
    ;Inserisco carattere per andare a capo e carrage return
    MOV DL, 0Ah
    MOV AH, 02
    INT 21h
    MOV DL, 0Dh
    INT 21h
   
    LEA DX, Errore
    MOV AH, 9
    INT 21h
    JMP Fine
     
    ;Somma 
Somma:
    MOV AL, Num1
    ADD AL, Num2
    MOV AH, 0
    MOV Ris, AX
    JMP Fine
    
    ;Sottrazione    
Sottrazione:
    MOV AL, Num1
    Sub AL, Num2
    Mov AH, 0
    MOV Ris, DX
    JMP Fine
    
    ;Moltiplicazione
Moltiplicazione:
    MOV AL, Num1
    MOV AH, 0
    MOV BL, Num2
    MUL BL
    MOV Ris, DX
    JMP Fine

    
Divisione:    
    MOV AL, Num1
    MOV BL, Num2
    DIV BL
    MOV AH, 0
    MOV Ris, DX
    JMP Fine

Fine:
    LEA DX, string
    MOV AH, 9
    INT 21h        ; output string at ds:dx
    
    ; wait for any key....    
    MOV AH, 1
    INT 21h
    
    MOV AX, 4c00h ; exit to operating system.
    INT 21h           
        
ends

NewLine proc near  
    
    MOV DL, 0Ah
    MOV AH, 02
    INT 21h
    MOV DL, 0Dh
    INT 21h 
    RET
    
endp

Stampa proc near
    
    POP BX
    LEA DX, BX
    MOV Ah, 9
    INT 21h
endp

end start ; set entry point and stop the assembler.        