global $a = Default + (Null * "42"), $b = 42, $c = 7


$x = "X"
$y = "Y"
ConsoleWrite($x & " " & $y & @CRLF)
swap($x,$y)
ConsoleWrite($x & " " & $y & @CRLF)


Func Swap(ByRef $vVar1, ByRef $vVar2)
   Local $vTemp = $vVar1
   $vVar1 = $vVar2
   $vVar2 = $vTemp
EndFunc

#include <L:\Projects.VisualStudio\AutoItInterpreter\new\test\test.au3>



local $xl = ObjCreate("Excel.Application")
With $xl
   .visible = 1
   ;with $y
   ;EndWith
    MsgBox(0, "", "msg")
EndWith

Exit 9









; Local $arr[] = [8, 4, 5, 9, 1]

ConsoleWrite()


$b = 9 + $xxxx
$c = test($b)
TEST(8)

ConsoleWrite($b & @CRLF & $c);

func test($b = 9)
   local const $a = -9
   $b = 42
endfunc

