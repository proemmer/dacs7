
# Tag syntax

The Tag syntax of dacs7 has the format:

`Area.Offset,DataType[,Length]`


## Area

dacs7 supports english and german mnemonic and also its own syntax (The input is not case sensitive)
Possible values for the area:

* **Input**:  i, e, I, E, IB, ib, Ib, iB
* **Marker**:  m, M, FB, Fb, fb, fB
* **Output**:  q, a, Q, A, Qa, QA, qa, qA
* **Timer**:  t, T, tm, Tm, TM, tM
* **Counter**: c, z, C, Z, CT, ct, Ct, cT
* **Datablock**: db1, Db1, DB1, dB1

## Offset

The offset in byte from the beginning of the specified area.

## Type

Possible values for the type:

* **x[bit]**:  Boolean  using a bit you also have to specify the bit number in the datatype part.
* **b**:       Byte
* **w**:       Word (ushort)
* **dw**:      DWord (uint)
* **lw**:      LongWord (ulong) 
* **si**:      SmallInt (sbyte)
* **i**:       Int (short)
* **di**:      DInt (int)
* **li**:      LongInt (long) 
* **r**:       Real (float)
* **c**:       Char
* **wc**:      WChar  (string)
* **s**:       String
* **ws**:      WString (string)

**Length**

This part is optional, the default value is 1. (a special case is the string type, this specifies the length of the string, so it is currently not possible to read a string array in one command);




## Samples


| Tag         | Area                 | offset |   data type    | length |
|:------------|:--------------------:|:------:|:--------------:|:------:|
|DB1.0,b,100  |  Datablock number 1  |    0   |      Byte      |   100  | 
|DB3.1,x0     |  Datablock number 3  |    1   |  Bool[bit 0]   |   1  | 
|DB3.1,x5     |  Datablock number 3  |    1   |  Bool[bit 5]   |   1  | 
|DB2.10,w     |  Datablock number 2  |   10   |     ushort     |   1  |
|DB2.10,i     |  Datablock number 2  |   10   |      short     |   1  |
|DB2.10,dw    |  Datablock number 2  |   10   |      uint      |   1  |
|DB2.10,di    |  Datablock number 2  |   10   |      int       |   1  |
|M.10,x0      |  Marker              |   10   |   Bool[bit 0]  |   1  |