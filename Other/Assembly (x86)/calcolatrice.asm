; multi-segment executable file template.

data segment
    ; add your data here!   
    Num1 DW 1A3Ch
    Num2 DW 1A00h
    Ris  DW 0000h 
    Iniz DW 0000h
    pkey db "inserisci 1 per la somma, 2 per la sottrazione, 3 per la moltiplicazione e 4 per la divisione$" 
    
ends

stack segment
    dw   128  dup(0)
ends

code segment
start:
; set segment registers:
    mov ax, data
    mov ds, ax
    mov ax, Num1
    mov bx, Num2
    

    ; add your code here     
    lea dx, pkey
    cmp dx,1
    jge salta
    add ax,bx;somma 
    jmp fine 
    
salta:
    
    cmp dx,2
    jge salta
    sub ax,bx;sottrazione 
    jmp fine
    


    cmp dx,3
    jge salta
    mul ax,bx;moltiplicazione 
    jmp fine  
    


    div ax,bx;divisione
    jmp fine 
    
fine:
                
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
