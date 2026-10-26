; multi-segment executable file template.

data segment
    Num1 DW 0008h
    Num2 DW 0006h
    Ris DW 0000h
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
    mov ax, 0
    mov bx, num2
while:
    add ax, Num1
    dec bx 
    cmp bx, 0 
    mov Ris, ax
    je stop
    jmp while
stop:            
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