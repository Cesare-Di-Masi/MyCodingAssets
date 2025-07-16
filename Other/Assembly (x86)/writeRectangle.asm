; multi-segment executable file template.

data segment
    write1 db "scrivere la lunghezza della base del rettangolo...$"
    write2 db "scrivere la lunghezza dell'altezza del rettangolo...$"
    erroremsg db "qualcosa è andato storto, riprovare$"
    
    base db 0h
    high db 0h
    
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
            
    lea dx, write1
    mov ah, 9
    int 21h
    call ReadNumber
    mov base,AL
    call NewLine
    mov ax,0h
    
        
    lea dx, write2
    mov ah, 9
    int 21h
    call ReadNumber
    mov high,AL
    call NewLine
    mov ax, 0h 
    
    mov CH,0h
    mov Cl,high
    mov DH, 0h
    mov BX,0h
    
    mostrarettangolo:
    mov Dl,2Ah
    call WriteRectangle
    call newLine
    loop mostrarettangolo
    
    
    
        
ends  

    ReadNumber proc near:
        jmp leggi
        errore:
        call NewLine
        lea dx,erroremsg
        mov ah,9
        int 21h
        call NewLine
        
        leggi:
        mov ah, 1
        int 21h
        
        cmp AL,30h
        jl errore
        cmp AL,39h
        jg errore
        sub AL,30h
        
        
        ret
    endp
    
    WriteRectangle proc near:
         
         mov BL,base
         
         scriviBase:
         dec BL
         cmp BL,0h
         jl esci
         MOV AH,02H
         int 21h
         jmp scriviBase
         
         esci:
        
        ret
    endp
    
    NewLine proc near:
        
        mov DL,0AH
        mov ah,2
        int 21h
        mov DL,0Dh
        mov ah,2
        int 21h
        
        ret
    endp
    
    


end start ; set entry point and stop the assembler.
