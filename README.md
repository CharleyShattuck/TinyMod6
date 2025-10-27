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
