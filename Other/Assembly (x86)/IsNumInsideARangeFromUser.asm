data segment

    Num1       DW 0h
    Num2       DW 0h
    Num3       DW 0h
    OldNum     DW 0h

    WriteStart db "Scrivere il valore basso dell'intervallo...$"
    WriteEnd   db "Scrivere il valore alto dell'intervallo...$"
    WriteNum   db "Scrivere il numero da controllare...$"
    WriteError db "Qualcosa è andato storo, riprovare...$"
    writeNotInside db "False$"
    writeInside db "True$"

ends

stack segment
    dw 128 dup(0)
ends

code segment
start:
    MOV AX, data
    MOV DS, AX
    MOV AX, stack
    MOV SS, AX
    
    LEA DX, WriteStart
    MOV AH, 9
    INT 21h
    CALL ReadLine

    MOV DX, 0h
    MOV Num1, BX
    MOV AX, 0h
    MOV BX, 0h

    LEA DX, WriteEnd
    MOV AH, 9
    INT 21h
    CALL ReadLine

    MOV DX, 0h
    MOV Num2, BX
    MOV AX, 0h
    MOV BX, 0h

    LEA DX, WriteNum
    MOV AH, 9
    INT 21h
    CALL ReadLine

    MOV Num3, BX

    CMP Num3, Num1
    JL notInside
    CMP Num3, Num2
    JG notInside
    JMP Inside

notInside:
    LEA DX, writeNotInside
    MOV AH, 9
    INT 21h
    JMP EndOfCheck

Inside:
    LEA DX, writeInside
    MOV AH, 9
    INT 21h

EndOfCheck:

ends

NewLine proc near:
    MOV DL, 0Ah
    MOV AH, 02
    INT 21h
    MOV DL, 00h
    RET
endp

ReadLine proc near:
    JMP loopReadLine

errorReadLine:
    LEA DX, writeError
    MOV AH, 09h
    INT 21h
    MOV DX, 0h

loopReadLine:
    MOV OldNum, 0h
    MOV AH, 01h
    INT 21h
    CMP AL, 20h
    JE exitReadLine
    CMP AL, 30h
    JL errorReadLine
    CMP AL, 39h
    JG errorReadLine

    SUB AL, 30h
    MOV AH, 0h
    MOV OldNum, AX
    MOV AX, 10h
    XCHG AX, BX
    MUL BX
    XCHG AX, BX
    ADD BX, OldNum
    JMP loopReadLine

exitReadLine:
    RET
endp