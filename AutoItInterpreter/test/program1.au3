﻿#include-once
#include <APIComConstants.au3>
#include 'header-1.au3'


Func f2($a, $b)
    consolewriteline($"function f2 was called with:\n\t\$a = $a\n\t\$b = $b")
EndFunc

#css
    System.Console.WriteLine($"top kek at {System.DateTime.Now:yyyy-MM-dd HH-mm-ss-ffffff}!");
#cse #ce



$old = 42

consolewriteline($"\$old = $old")

$old += 88

for $cnt1 = 0 to 7
    f2($cnt * 2, $old)

    $old = $cnt
next
