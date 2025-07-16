; multi-segment executable file template.

data segment
    ; add your data here!
    pkey db "scrivere una string acomposta solamente da letter minuscole o maiuscole (premere invio per terminare)...$"
    startString db 1h ;valore da cui iniziera la stringa
ends

stack segment
    dw   128  dup(0)
ends

code segment
start:
; set segment registers:
    mov ax, data
    mov ds, ax
    mov es, ax

    ; add your code here
            
    lea dx, pkey
    mov ah, 9
    int 21h        ; output string at ds:dx
    
    call ReadString
    call NewLine
    call WriteString
    
ends

ReadString proc near:
    
    MOV SI,0
    
    jmp loopReadString
    WriteStringError:
    loopReadString:
    
    mov ah,1
    int 21h
    
    cmp AL,0Dh
    je exitReadStringLoop
    
    CMP AL,41h
    jl WriteStringError
    CMP AL,5Ah
    jg  nextCheck:
    jmp noError
    nextCheck:
    CMP AL,61h
    jl WriteStringError
    CMP AL,7Ah
    jg WriteStringError
    noError:
    
    mov startString[SI],AL
    inc SI
    jmp loopReadString
    exitReadStringLoop:
    inc SI
    mov startString[SI],24h
    MOV CX,SI
    
    ret
endp

WriteString proc near:
    lea DX,startString
    mov ah,09h
    int 21h
    
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
