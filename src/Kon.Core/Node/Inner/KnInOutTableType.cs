
using System;
using System.Collections.Generic;
using System.Linq;
using Kon.Core.Converter;

namespace Kon.Core.Node.Inner;

public enum KnInOutTableType
{
    // |!T1 a !T2 b|
    // |a (:+ 1 2)|
    NoOutput,
    // |!T1 a !T2 b -> T2 T1|
    OnlyTypeOutput,
    // |!T1 a !T2 b -- !T2 b !T1 a|
    NameAndTypeOutput,
}