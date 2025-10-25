\ main.fs  TinyMod6 firmware
target

\ Arduino constants
0 wconstant INPUT
1 wconstant OUTPUT
2 wconstant INPUT_PULLUP
\ use 1 for HIGH
\ and 0 for LOW

\ serial mode's array 
variable data 4 ramALLOT \ 6 bytes in all
: /data  data a! 5 #, for false c!+ next ; 

: initPortExpander  $20 #, initMCP23017 ;
: initPins  INPUT_PULLUP
    dup  5 #, pinMode
    dup  7 #, pinMode
    dup  9 #, pinMode
    dup 10 #, pinMode
    dup 11 #, pinMode
    dup 12 #, pinMode
    dup 18 #, pinMode
    dup 19 #, pinMode
    dup 20 #, pinMode
    dup 21 #, pinMode
    dup 22 #, pinMode
        23 #, pinMode
    ;
: init  initPortExpander initPins ;

\ read the keyboard
:m pin|  #, #, @pin and or m;
: @pins ( - n)
    $20 #, @MCP23017 0 #,
     9  $010000 pin|
    10  $020000 pin|
    11  $040000 pin|
    12  $080000 pin|
    19  $100000 pin|
    20  $200000 pin|
    21  $400000 pin|
    22  $800000 pin|
    23 $1000000 pin|
    or $1ff0000 #, xor ;

\ get one stroke
variable stroke
: press (  - n)  false begin drop @pins until ;
: release ( n1 - n2)  begin @pins while or repeat drop ;
: scan (  - n)
    begin press 30 #, ms @pins if or release exit then drop drop again

\ vectored emit, for testing
wvariable 'spit  \ execution tokens are 16 bits
: spit  'spit w@ execute ; 
: >emit  ['] emit 'spit w! ; 
: >hc.  ['] hc. 'spit w! ; 
: send  data a! 5 #, for c@+ spit next ; \ Gemini
: ?send  data a!  \ TX Bolt
    c@+ if dup spit then drop
    c@+ if dup $40 #, or spit then drop
    c@+ if dup $80 #, or spit then drop
    c@+ if $c0 #, or spit exit then spit ;

\ for either Gemini or Bolt
: mark ( mask i)  data + dup >r c@ or r> c! ;
: ?mark ( mask i key)
    stroke @ and if  drop mark exit then
    drop drop drop ;
:m _mark  #, #, #, ?mark m;

\ Gemini protocol to the data array
: Gemini ( n)  stroke !  /data
    $80 #, data c!
    $0100000 1 $40 _mark  \ S1
    $0200000 1 $10 _mark  \ T
    $0400000 1 $04 _mark  \ P
    $0800000 1 $01 _mark  \ H
    $1000000 2 $08 _mark  \ *
    $0008000 3 $02 _mark  \ F
    $0004000 4 $40 _mark  \ P
    $0002000 4 $10 _mark  \ L
    $0001000 4 $04 _mark  \ T
    $0000100 4 $01 _mark  \ D
    $0080000 1 $20 _mark  \ S2
    $0040000 1 $08 _mark  \ K
    $0020000 1 $02 _mark  \ W
    $0010000 2 $40 _mark  \ R
    $0000200 2 $04 _mark  \ *
    $0000001 3 $01 _mark  \ R
    $0000002 4 $20 _mark  \ B
    $0000004 4 $08 _mark  \ G
    $0000800 4 $02 _mark  \ S
    $0000400 5 $01 _mark  \ Z
    $0000008 2 $20 _mark  \ A
    $0000010 2 $10 _mark  \ O 
    $0000020 5 $40 _mark  \ #
    $0000040 3 $08 _mark  \ E
    $0000080 3 $04 _mark  \ U
    ;
: send-Gemini  Gemini send ;

\ TX Bolt protocol to the data array
: bolt ( n)  stroke !  /data
    $0100000 0 $01 _mark  \ S1
    $0080000 0 $01 _mark  \ S2
    $0200000 0 $02 _mark  \ T
    $0040000 0 $04 _mark  \ K
    $0400000 0 $08 _mark  \ P
    $0020000 0 $10 _mark  \ W
    $0800000 0 $20 _mark  \ H
    $0010000 1 $01 _mark  \ R
    $0000008 1 $02 _mark  \ A
    $0000010 1 $04 _mark  \ O 
    $1000000 1 $08 _mark  \ *
    $0000200 1 $08 _mark  \ *
    $0000040 1 $10 _mark  \ E
    $0000080 1 $20 _mark  \ U
    $0008000 2 $01 _mark  \ F
    $0000001 2 $02 _mark  \ R
    $0004000 2 $04 _mark  \ P
    $0000002 2 $08 _mark  \ B
    $0002000 2 $10 _mark  \ L
    $0000004 2 $20 _mark  \ G
    $0001000 3 $01 _mark  \ T
    $0000800 3 $02 _mark  \ S
    $0000100 3 $04 _mark  \ D
    $0000400 3 $08 _mark  \ Z
    $0000020 3 $10 _mark  \ #
    ;
: send-TXBolt  bolt ?send ;

\ A-Z
: spout ( stroke char mask - stroke )
    2 #, ms
    stroke @ and if drop Keyboard.write exit then
    drop drop BL Keyboard.write ;
:m --> ( char mask - )  char #, #, spout m;
: send-A-Z ( stroke)
    stroke ! 
    2 #, ms char > #, Keyboard.write
    $0000020 --> #
    $0180000 --> S
    $0200000 --> T
    $0040000 --> K
    $0400000 --> P
    $0020000 --> W
    $0800000 --> H
    $0010000 --> R
    $0000008 --> A
    $0000010 --> O
    $1000200 --> *
    $0000040 --> E
    $0000080 --> U
    $0008000 --> F
    $0000001 --> R
    $0004000 --> P
    $0000002 --> B
    $0002000 --> L
    $0000004 --> G
    $0001000 --> T
    $0000800 --> S
    $0000100 --> D
    $0000400 --> Z
    5 #, ms 10 #, Keyboard.write
    5 #, ms 13 #, Keyboard.write
    ;

\ NKRO keyboard mode
cvariable former
: spew ( c - )
    dup Keyboard.press
    former c@ if dup Keyboard.release then
    drop former c! ;
: ?spew ( c n)
    stroke @ and if over spew then  drop drop ;
:m -->  char #, #, ?spew m;
: send-NKRO ( n)
    stroke !  false former c!
     $100000 --> q
     $200000 --> w
     $400000 --> e
     $800000 --> r
    $1000000 --> t
       $8000 --> u
       $4000 --> i
       $2000 --> o
       $1000 --> p
        $100 --> [
      $80000 --> a
      $40000 --> s
      $20000 --> d
      $10000 --> f
        $200 --> g
         $01 --> j
         $02 --> k
         $04 --> l
        $800 --> ;
        $400 --> '
         $08 --> c
         $10 --> v
         $20 --> 3
         $40 --> n
         $80 --> m
    Keyboard.releaseAll ;

\ slider switch determines the protocol
: @sliders ( - n)
   18 #, @pin invert 2 #, and
    7 #, @pin invert 1 #, and or ;

create protocols
    ', send-NKRO
    ', send-Gemini
    ', send-TXBolt
    ', send-A-Z

turnkey decimal init
0 [if]  \ interpreter
    >hc. begin interpret again
[then]
1 [if]  \ application
    Keyboard.begin >emit
    begin  scan @sliders protocols + @p execute
    again
[then]

