# TinyMod6 firmware for the TinyMod6 steno keyboard
ITC Forth in C and Forth for the AVR 32u4. 32 bit stacks, 16 bit program memory. The VM is written with Arduino Wiring, and Forth core and app are written using a target compiler written in gforth. The Forth compiler's output is a memory array that's used when compiling the VM. I hope that makes sense. 32 bit stacks meant to make this particular program easier to write.

## Architecture
This is a target compiled Forth, made in two parts. The first part is done using gforth to make the higher level Forth. It's indirect threaded, meaning each Forth word is preceded by a code field, which contains a pointer to the common code for a class of words. This pointer actually is an index into a table of functions written in C and residing in a separate memory space. Since the target application runs in an AVR 32u4 microcontroller, the program memory space is in flash memory and so is the high level Forth code. Think of the high level Forth code as data. It's a C array of 16 bit words. When Forth code is compiled it becomes a file called memory.h to be used when compiling the C part using the Arduino compiler.

## compiler.fs
The key points of the target compiler are probably the words *,* and *save*. A memory space is declared with
```
create target-image target-size allot
```
and , is defined to compile a 16 bit word into the next location in that space. All the rest of the high level Forth has , at the bottom.

Save is run at the end to make a file called memory.h which, when compiled in Arduino, creates an array. This array is the high level Forth code memory visible to the Forth programmer.

## core.fs
Forth code not specific to the app is in core.fs. The word *code* is a defining word. It creates a header (separate from the code field and body of the word) and compiles the index into an array of C functions defined in the file TinyMod6.ino. I'll talk about this file later. This is a departure from what I think of as indirect threaded code. When code and data are able to be in the same space the code field of a code word points to code in that space, usually just following the code field. In this case the actual code is in another space, so the code field contains an index into that space. This file contains an interpreter that runs on the target. The headers are separate from the code fields and body of each word. The headers build down from the top of program memory while compiling in Forth. It's possible to eliminate them to save memory if that's important. This core is meant to contain all primitives needed for the application. If used for another application maybe more words would need to be added here.

I'd like to point out a few differences from standard Forth here. Having worked with colorForth and arrayForth for a number of years I like to replace DO LOOP with FOR NEXT. Also the conditionals do not drop the top of stack. I've added address registers for both code memory and data memory (RAM) much like the A and B registers in the Green Arrays chips. This makes possible words like are *@a+*, *!a+*, and *@p+*, where the address register is incremented after the fetch or store. These work nicely in loops. I don't know how to actually use registers in C, so these are really just variables in the C code. They look like registers to the application programmer using Forth.

## TinyMod6.ino
This is the file compiled by Arduino. It starts by including memory.h which was created when the Forth code was compiled in gforth. It also includes Wire.h which is library code for the i2c interface and Keyboard.h which is library code for the USB keyboard interface. This is why I'm using C at all, because I don't want to figure out how to code the USB keyboard in assembly code. Also its been fun to mix the old ITC Forth and C. It could go without saying that I don't want to just use C for everything.

Then a lot of registers are defined. They are C variables of various sizes. They are:
* T top of data stack, 32 bits for this app, to hold a complete keyboard stroke
* N second on data stack, not preserved but often used within a word, also 32 bits
* I interpreter pointer, part of the virtual machine, 16 bit word pointer into code memory
* W working register, also part of the virtual machine, 16 bit word pointer into code memory
* S data stack pointer, 8 bits
* R return stack pointer, 8 bits
* P 16 bit word pointer into code memory, in ROM
* S 16 bits, but addressing RAM in bytes
* elapsed a large counter for interactive timing
* D a 64 bit register used to store intermediate results of multiplication for */

Then the stacks are defined as well as arrays of data memory in RAM. *setup()* is defined for the Arduino compiler. Then all the code for the Forth words is defined as C functions without arguments. These functions use the global "registers" of the Forth virtual machine.

These functions are put into an executable array of functions, to be used in the main loop of the virtual machine, called *next:*. The *next* loop, inside the Arduino *loop()* function, reads the next word of memory pointed to by the I register into the W register and increments the I register. Then the W register is used as an index into the function array of Forth primitive functions and executes the indexed function. The W register is also incremented to point just past the word being executed, in case it happens to be a useful data field. Once that primitive has been executed control returns here and we jump to *next* again, in an endless loop. That's the virtual machine main loop. It might also be called the inner interpreter.

## main.fs
The application source goes into the main.fs file. The last part of this file has the turnkey code, in two sections. The first section is the outer interpreter, for the interactive testing. It's commented out when you want the the application to run. The second part initializes the application and then falls into the infinite loop of the application.
