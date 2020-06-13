// Autogenerated 2020-06-13 16:59:45.285

namespace Unknown6656.AutoIt3.ExpressionParser

open Piglet.Parser.Configuration.Generic
open System

[<AutoOpen>]
module ParsingUtil =
    let internal reducef (s : NonTerminalWrapper<'a>) x =
        s.AddProduction().SetReduceFunction (Func<'a>(x))
        |> ignore

    // let internal reduce0 (s : NonTerminalWrapper<'a>) a =
    //     s.AddProduction(a).SetReduceToFirst()
    //     |> ignore

    let internal reduce1 (s : NonTerminalWrapper<'a>) a x =
        s.AddProduction(a).SetReduceFunction (Func<_, 'a>(x))
        |> ignore

    let internal reduce2 (s : NonTerminalWrapper<'a>) a b x =
        s.AddProduction(a, b).SetReduceFunction (Func<_, _, 'a>(x))
        |> ignore

    let internal reduce3 (s : NonTerminalWrapper<'a>) a b c x =
        s.AddProduction(a, b, c).SetReduceFunction (Func<_, _, _, 'a>(x))
        |> ignore

    let internal reduce4 (s : NonTerminalWrapper<'a>) a b c d x =
        s.AddProduction(a, b, c, d).SetReduceFunction (Func<_, _, _, _, 'a>(x))
        |> ignore

    let internal reduce5 (s : NonTerminalWrapper<'a>) a b c d e x =
        s.AddProduction(a, b, c, d, e).SetReduceFunction (Func<_, _, _, _, _, 'a>(x))
        |> ignore

    let internal reduce6 (s : NonTerminalWrapper<'a>) a b c d e f x =
        s.AddProduction(a, b, c, d, e, f).SetReduceFunction (Func<_, _, _, _, _, _, 'a>(x))
        |> ignore

    let internal reduce7 (s : NonTerminalWrapper<'a>) a b c d e f g x =
        s.AddProduction(a, b, c, d, e, f, g).SetReduceFunction (Func<_, _, _, _, _, _, _, 'a>(x))
        |> ignore

    let internal reduce8 (s : NonTerminalWrapper<'a>) a b c d e f g h x =
        s.AddProduction(a, b, c, d, e, f, g, h).SetReduceFunction (Func<_, _, _, _, _, _, _, _, 'a>(x))
        |> ignore

    let internal reduce9 (s : NonTerminalWrapper<'a>) a b c d e f g h i x =
        s.AddProduction(a, b, c, d, e, f, g, h, i).SetReduceFunction (Func<_, _, _, _, _, _, _, _, _, 'a>(x))
        |> ignore

    let internal reduce10 (s : NonTerminalWrapper<'a>) a b c d e f g h i j x =
        s.AddProduction(a, b, c, d, e, f, g, h, i, j).SetReduceFunction (Func<_, _, _, _, _, _, _, _, _, _, 'a>(x))
        |> ignore

    let internal reduce0 s a = reduce1 s a id
