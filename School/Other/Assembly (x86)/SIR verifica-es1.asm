; multi-segment executable file template.

data segment
    
    WritePin db "scrivere 4 numeri...$"
    WriteKey db "scrivere un numero compreso da 1 a 20 (premere spazio per terminare la scrittura)...$"
    WriteKeyOutOfRange db "la chiave di cifratura deve essere fra 1 e 20...$"
    
    Key Db 0h
    
    RPin Db 0,0,0,0 ;pin non cifrato
    CPin Db 0,0,0,0 ;pin cifrato
    
    oldNum Db 0h
    
ends

stack segment
    dw   128  dup(0)
ends

code segment
start:
; set segment registers:
    mov ax, data
    mov ds, ax
    mov ax,stack
    mov ss, ax

    ; add your code here
            
    lea dx, WritePin
    mov ah, 9
    int 21h
    call ReadPin
    
    lea dx, WriteKey
    mov ah,9
    int 21h
    call ReadKey
    
    mov Key,BL
    
    call CodePins
    
    call WritePins
    
    ; wait for any key....    
    
    
    mov ax, 4c00h ; exit to operating system.
    int 21h    
ends

    ReadPin proc near:;procedura per la lettura del pin
        Mov CX,4
        Mov SI,0
        jmp readPinLoop
        readPinError:
        readPinLoop:
        
        mov ah, 1
        int 21h
        cmp AL,30h
        jl readPinError
        cmp AL,39h
        jg readPinError
        
        SUB AL,30h
        
        mov RPin[SI],AL
        
        inc SI
        
        loop readPinLoop
        
        
        call NewLine
        ret
    endp
    
    ReadKey proc near:  ;procedura per la lettura della chiave
        jmp readKeyLoop
        
        WriteOutOfRangeError:
        call NewLine
        lea DX,WriteKeyOutOfRange
        mov ah,9
        int 21h
        Mov BX,0h
        jmp readKeyLoop
        
        call NewLine
        
        lea DX,WriteKey
        mov ah,9
        int 21h
        
        call NewLine
        
        readKeyError:
        readKeyLoop:
        mov ah,1
        int 21h
        
        cmp AL,20h   ;se l'utente digita lo spazio questo uscira dal loop
        je exitReadKey
        
        cmp AL,30h
        jl readKeyError
        cmp AL,39h
        jg readKeyError
        SUB AL,30h
        
        mov OldNum,AL
        
        mov AL,10
        
        xchg BL,AL
        mul BL
        xchg BL,AL
        ADD BL,OldNum
        
        cmp BL,20
        jg  WriteOutOfRangeError:
        cmp BL,1
        jl WriteOutOfRangeError
        
        jmp readKeyLoop
        
        exitReadKey:
        
        call NewLine
        
        ret
    endp
    
    CodePins proc near:;procedura per la codifica del pin
        Mov CX,4
        Mov SI,0
        loopCodePins:
        mov BH,RPin[SI]
        ADD BH,Key
        mov CPin[SI],BH
        inc SI
        loop loopCodePins
        ret
    endp
    
    WritePins proc near:;scrittura di entrambi i pin
        MOV CX,4
        MOV SI,0
        
        loopPrintDecodePin: ;scrivo il pin decodificato
        
        mov BL,RPin[SI]
        add BL,30h
        MOV DL,BL
        mov ah,02h
        int 21h
        
        INC SI
        
        loop loopPrintDecodePin
        
        call NewLine
        
        MOV CX,4
        MOV SI,0
        
        loopPrintCodedPin: ;scrivo il pin codificato
        mov BL,CPin[SI]
        add BL,30h
        mov DL,BL
        mov ah,02h
        int 21h
        
        inc SI
        
        loop loopPrintCodedPin
        
        ret
    endp
    
    NewLine proc near:;procedura per andare a capo
        mov DL,0Ah
        mov Ah,02h
        int 21h
        
        mov DL,0Dh
        mov ah,02h
        int 21h 
               
        ret
    endp
    



end start ; set entry point and stop the assembler.
