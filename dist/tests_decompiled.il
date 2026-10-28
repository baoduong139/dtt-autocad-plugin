
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern DTools
{
  .ver 1:0:0:0
}
.assembly DTools.Tests
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 

  .custom instance void [mscorlib]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 1A 2E 4E 45 54 46 72 61 6D 65 77 6F 72 6B   // ....NETFramework
                                                                                                        2C 56 65 72 73 69 6F 6E 3D 76 34 2E 38 01 00 54   // ,Version=v4.8..T
                                                                                                        0E 14 46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C   // ..FrameworkDispl
                                                                                                        61 79 4E 61 6D 65 12 2E 4E 45 54 20 46 72 61 6D   // ayName..NET Fram
                                                                                                        65 77 6F 72 6B 20 34 2E 38 )                      // ework 4.8
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module DTools.Tests.exe
// MVID: {73143608-3BEB-4ADB-B2DE-03B0148B6BED}
.imagebase 0x0000000140000000
.file alignment 0x00000200
.stackreserve 0x0000000000400000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x026E0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit DTools.Tests.Program
       extends [mscorlib]System.Object
{
  .class auto ansi serializable sealed nested private beforefieldinit '<>c'
         extends [mscorlib]System.Object
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public static initonly class DTools.Tests.Program/'<>c' '<>9'
    .field public static class [mscorlib]System.Action '<>9__0_0'
    .field public static class [mscorlib]System.Action '<>9__0_1'
    .field public static class [mscorlib]System.Action '<>9__0_2'
    .field public static class [mscorlib]System.Action '<>9__0_3'
    .field public static class [mscorlib]System.Action '<>9__0_4'
    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  newobj     instance void DTools.Tests.Program/'<>c'::.ctor()
      IL_0005:  stsfld     class DTools.Tests.Program/'<>c' DTools.Tests.Program/'<>c'::'<>9'
      IL_000a:  ret
    } // end of method '<>c'::.cctor

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method '<>c'::.ctor

    .method assembly hidebysig instance void 
            '<Main>b__0_0'() cil managed
    {
      // Code size       244 (0xf4)
      .maxstack  2
      .locals init (class [DTools]DTools.Core.LightingPlanModel V_0)
      IL_0000:  newobj     instance void [DTools]DTools.Core.LightingPlanModel::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  callvirt   instance class [DTools]DTools.Core.ValidationResult [DTools]DTools.Core.LightingPlanModel::ValidateGeometry()
      IL_000c:  callvirt   instance bool [DTools]DTools.Core.ValidationResult::get_IsValid()
      IL_0011:  ldstr      bytearray (48 00 EC 00 6E 00 68 00 20 00 68 00 CD 1E 63 00   // H...n.h. .h...c.
                                      20 00 6D 00 B7 1E 74 00 20 00 62 00 B1 1E 6E 00   //  .m...t. .b...n.
                                      67 00 20 00 6D 00 B7 1E 63 00 20 00 11 01 CB 1E   // g. .m...c. .....
                                      6E 00 68 00 20 00 70 00 68 00 A3 1E 69 00 20 00   // n.h. .p.h...i. .
                                      68 00 E3 1E 70 00 20 00 6C 00 C7 1E 2E 00 )       // h...p. .l.....
      IL_0016:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_001b:  ldloc.0
      IL_001c:  callvirt   instance float64 [DTools]DTools.Core.LightingPlanModel::get_InteriorLengthMm()
      IL_0021:  ldc.r8     12000.
      IL_002a:  ceq
      IL_002c:  ldstr      bytearray (4B 00 ED 00 63 00 68 00 20 00 74 00 68 00 B0 01   // K...c.h. .t.h...
                                      DB 1E 63 00 20 00 6C 00 CD 1E 74 00 20 00 6C 00   // ..c. .l...t. .l.
                                      F2 00 6E 00 67 00 20 00 64 00 E0 00 69 00 20 00   // ..n.g. .d...i. .
                                      70 00 68 00 A3 1E 69 00 20 00 62 00 B1 1E 6E 00   // p.h...i. .b...n.
                                      67 00 20 00 31 00 32 00 30 00 30 00 30 00 20 00   // g. .1.2.0.0.0. .
                                      6D 00 6D 00 2E 00 )                               // m.m...
      IL_0031:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_0036:  ldloc.0
      IL_0037:  callvirt   instance float64 [DTools]DTools.Core.LightingPlanModel::get_InteriorWidthMm()
      IL_003c:  ldc.r8     6000.
      IL_0045:  ceq
      IL_0047:  ldstr      bytearray (4B 00 ED 00 63 00 68 00 20 00 74 00 68 00 B0 01   // K...c.h. .t.h...
                                      DB 1E 63 00 20 00 6C 00 CD 1E 74 00 20 00 6C 00   // ..c. .l...t. .l.
                                      F2 00 6E 00 67 00 20 00 72 00 D9 1E 6E 00 67 00   // ..n.g. .r...n.g.
                                      20 00 70 00 68 00 A3 1E 69 00 20 00 62 00 B1 1E   //  .p.h...i. .b...
                                      6E 00 67 00 20 00 36 00 30 00 30 00 30 00 20 00   // n.g. .6.0.0.0. .
                                      6D 00 6D 00 2E 00 )                               // m.m...
      IL_004c:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_0051:  ldloc.0
      IL_0052:  callvirt   instance class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>> [DTools]DTools.Core.LightingPlanModel::get_LightPositions()
      IL_0057:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>>::get_Count()
      IL_005c:  ldc.i4.2
      IL_005d:  ceq
      IL_005f:  ldstr      bytearray (50 00 68 00 A3 1E 69 00 20 00 63 00 F3 00 20 00   // P.h...i. .c... .
                                      11 01 FA 00 6E 00 67 00 20 00 32 00 20 00 11 01   // ....n.g. .2. ...
                                      E8 00 6E 00 20 00 74 00 72 00 A7 1E 6E 00 2E 00 ) // ..n. .t.r...n...
      IL_0064:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_0069:  ldloc.0
      IL_006a:  callvirt   instance class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>> [DTools]DTools.Core.LightingPlanModel::get_LightPositions()
      IL_006f:  ldc.i4.0
      IL_0070:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>>::get_Item(int32)
      IL_0075:  callvirt   instance !0 class [mscorlib]System.Tuple`2<float64,float64>::get_Item1()
      IL_007a:  ldc.r8     4000.
      IL_0083:  bne.un.s   IL_00a3

      IL_0085:  ldloc.0
      IL_0086:  callvirt   instance class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>> [DTools]DTools.Core.LightingPlanModel::get_LightPositions()
      IL_008b:  ldc.i4.0
      IL_008c:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>>::get_Item(int32)
      IL_0091:  callvirt   instance !1 class [mscorlib]System.Tuple`2<float64,float64>::get_Item2()
      IL_0096:  ldc.r8     3000.
      IL_009f:  ceq
      IL_00a1:  br.s       IL_00a4

      IL_00a3:  ldc.i4.0
      IL_00a4:  ldstr      bytearray (10 01 E8 00 6E 00 20 00 31 00 20 00 70 00 68 00   // ....n. .1. .p.h.
                                      A3 1E 69 00 20 00 DF 1E 20 00 74 00 CD 1E 61 00   // ..i. ... .t...a.
                                      20 00 11 01 D9 1E 20 00 28 00 34 00 30 00 30 00   //  ..... .(.4.0.0.
                                      30 00 2C 00 20 00 33 00 30 00 30 00 30 00 29 00   // 0.,. .3.0.0.0.).
                                      2E 00 )                                           // ..
      IL_00a9:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_00ae:  ldloc.0
      IL_00af:  callvirt   instance class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>> [DTools]DTools.Core.LightingPlanModel::get_LightPositions()
      IL_00b4:  ldc.i4.1
      IL_00b5:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>>::get_Item(int32)
      IL_00ba:  callvirt   instance !0 class [mscorlib]System.Tuple`2<float64,float64>::get_Item1()
      IL_00bf:  ldc.r8     10000.
      IL_00c8:  bne.un.s   IL_00e8

      IL_00ca:  ldloc.0
      IL_00cb:  callvirt   instance class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>> [DTools]DTools.Core.LightingPlanModel::get_LightPositions()
      IL_00d0:  ldc.i4.1
      IL_00d1:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>>::get_Item(int32)
      IL_00d6:  callvirt   instance !1 class [mscorlib]System.Tuple`2<float64,float64>::get_Item2()
      IL_00db:  ldc.r8     3000.
      IL_00e4:  ceq
      IL_00e6:  br.s       IL_00e9

      IL_00e8:  ldc.i4.0
      IL_00e9:  ldstr      bytearray (10 01 E8 00 6E 00 20 00 32 00 20 00 70 00 68 00   // ....n. .2. .p.h.
                                      A3 1E 69 00 20 00 DF 1E 20 00 74 00 CD 1E 61 00   // ..i. ... .t...a.
                                      20 00 11 01 D9 1E 20 00 28 00 31 00 30 00 30 00   //  ..... .(.1.0.0.
                                      30 00 30 00 2C 00 20 00 33 00 30 00 30 00 30 00   // 0.0.,. .3.0.0.0.
                                      29 00 2E 00 )                                     // )...
      IL_00ee:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_00f3:  ret
    } // end of method '<>c'::'<Main>b__0_0'

    .method assembly hidebysig instance void 
            '<Main>b__0_1'() cil managed
    {
      // Code size       122 (0x7a)
      .maxstack  6
      IL_0000:  newobj     instance void [DTools]DTools.Core.LightingPlanModel::.ctor()
      IL_0005:  dup
      IL_0006:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>>::.ctor()
      IL_000b:  dup
      IL_000c:  ldc.r8     -100.
      IL_0015:  ldc.r8     3000.
      IL_001e:  call       class [mscorlib]System.Tuple`2<!!0,!!1> [mscorlib]System.Tuple::Create<float64,float64>(!!0,
                                                                                                                   !!1)
      IL_0023:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>>::Add(!0)
      IL_0028:  dup
      IL_0029:  ldc.r8     5000.
      IL_0032:  ldc.r8     6100.
      IL_003b:  call       class [mscorlib]System.Tuple`2<!!0,!!1> [mscorlib]System.Tuple::Create<float64,float64>(!!0,
                                                                                                                   !!1)
      IL_0040:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>>::Add(!0)
      IL_0045:  callvirt   instance void [DTools]DTools.Core.LightingPlanModel::set_LightPositions(class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Tuple`2<float64,float64>>)
      IL_004a:  callvirt   instance class [DTools]DTools.Core.ValidationResult [DTools]DTools.Core.LightingPlanModel::ValidateGeometry()
      IL_004f:  dup
      IL_0050:  callvirt   instance bool [DTools]DTools.Core.ValidationResult::get_IsValid()
      IL_0055:  ldc.i4.0
      IL_0056:  ceq
      IL_0058:  ldstr      bytearray (42 00 D9 1E 20 00 6B 00 69 00 C3 1E 6D 00 20 00   // B... .k.i...m. .
                                      74 00 72 00 61 00 20 00 70 00 68 00 A3 1E 69 00   // t.r.a. .p.h...i.
                                      20 00 62 00 E1 00 6F 00 20 00 6C 00 D7 1E 69 00   //  .b...o. .l...i.
                                      20 00 6B 00 68 00 69 00 20 00 11 01 E8 00 6E 00   //  .k.h.i. .....n.
                                      20 00 6E 00 B1 1E 6D 00 20 00 6E 00 67 00 6F 00   //  .n...m. .n.g.o.
                                      E0 00 69 00 20 00 70 00 68 00 F2 00 6E 00 67 00   // ..i. .p.h...n.g.
                                      2E 00 )                                           // ..
      IL_005d:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_0062:  callvirt   instance class [mscorlib]System.Collections.Generic.List`1<string> [DTools]DTools.Core.ValidationResult::get_Errors()
      IL_0067:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<string>::get_Count()
      IL_006c:  ldc.i4.2
      IL_006d:  ceq
      IL_006f:  ldstr      bytearray (50 00 68 00 A3 1E 69 00 20 00 62 00 AF 1E 74 00   // P.h...i. .b...t.
                                      20 00 11 01 B0 01 E3 1E 63 00 20 00 32 00 20 00   //  .......c. .2. .
                                      6C 00 D7 1E 69 00 20 00 11 01 E8 00 6E 00 20 00   // l...i. .....n. .
                                      6E 00 67 00 6F 00 E0 00 69 00 20 00 74 00 B0 01   // n.g.o...i. .t...
                                      DD 1E 6E 00 67 00 2E 00 )                         // ..n.g...
      IL_0074:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_0079:  ret
    } // end of method '<>c'::'<Main>b__0_1'

    .method assembly hidebysig instance void 
            '<Main>b__0_2'() cil managed
    {
      // Code size       133 (0x85)
      .maxstack  3
      IL_0000:  newobj     instance void [DTools]DTools.Core.DolStarterConfig::.ctor()
      IL_0005:  dup
      IL_0006:  ldc.i4.0
      IL_0007:  callvirt   instance void [DTools]DTools.Core.DolStarterConfig::set_Mode(valuetype [DTools]DTools.Core.StarterMode)
      IL_000c:  dup
      IL_000d:  callvirt   instance string [DTools]DTools.Core.DolStarterConfig::get_HoldingContactNoTerminals()
      IL_0012:  ldstr      "21-22"
      IL_0017:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                     string)
      IL_001c:  ldstr      bytearray (43 00 68 00 BF 1E 20 00 11 01 D9 1E 20 00 68 00   // C.h... ..... .h.
                                      EC 00 6E 00 68 00 20 00 6D 00 AB 1E 75 00 20 00   // ..n.h. .m...u. .
                                      70 00 68 00 A3 1E 69 00 20 00 67 00 69 00 EF 1E   // p.h...i. .g.i...
                                      20 00 6E 00 67 00 75 00 79 00 EA 00 6E 00 20 00   //  .n.g.u.y...n. .
                                      63 00 68 00 E2 00 6E 00 20 00 32 00 31 00 2D 00   // c.h...n. .2.1.-.
                                      32 00 32 00 2E 00 )                               // 2.2...
      IL_0021:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_0026:  call       class [DTools]DTools.Core.ValidationResult [DTools]DTools.Core.CircuitRuleChecker::ValidateDolCircuit(class [DTools]DTools.Core.DolStarterConfig)
      IL_002b:  callvirt   instance class [mscorlib]System.Collections.Generic.List`1<string> [DTools]DTools.Core.ValidationResult::get_Warnings()
      IL_0030:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<string>::get_Count()
      IL_0035:  ldc.i4.0
      IL_0036:  cgt
      IL_0038:  ldstr      bytearray (43 00 68 00 BF 1E 20 00 11 01 D9 1E 20 00 68 00   // C.h... ..... .h.
                                      EC 00 6E 00 68 00 20 00 6D 00 AB 1E 75 00 20 00   // ..n.h. .m...u. .
                                      70 00 68 00 A3 1E 69 00 20 00 63 00 F3 00 20 00   // p.h...i. .c... .
                                      63 00 A3 1E 6E 00 68 00 20 00 62 00 E1 00 6F 00   // c...n.h. .b...o.
                                      20 00 74 00 69 00 BF 1E 70 00 20 00 11 01 69 00   //  .t.i...p. ...i.
                                      C3 1E 6D 00 20 00 32 00 31 00 2D 00 32 00 32 00   // ..m. .2.1.-.2.2.
                                      2E 00 )                                           // ..
      IL_003d:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_0042:  newobj     instance void [DTools]DTools.Core.DolStarterConfig::.ctor()
      IL_0047:  dup
      IL_0048:  ldc.i4.1
      IL_0049:  callvirt   instance void [DTools]DTools.Core.DolStarterConfig::set_Mode(valuetype [DTools]DTools.Core.StarterMode)
      IL_004e:  dup
      IL_004f:  callvirt   instance string [DTools]DTools.Core.DolStarterConfig::get_HoldingContactNoTerminals()
      IL_0054:  ldstr      "13-14"
      IL_0059:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                     string)
      IL_005e:  ldstr      bytearray (43 00 68 00 BF 1E 20 00 11 01 D9 1E 20 00 43 00   // C.h... ..... .C.
                                      68 00 75 00 A9 1E 6E 00 20 00 6B 00 F9 1E 20 00   // h.u...n. .k... .
                                      74 00 68 00 75 00 AD 1E 74 00 20 00 70 00 68 00   // t.h.u...t. .p.h.
                                      A3 1E 69 00 20 00 73 00 ED 1E 61 00 20 00 74 00   // ..i. .s...a. .t.
                                      69 00 BF 1E 70 00 20 00 11 01 69 00 C3 1E 6D 00   // i...p. ...i...m.
                                      20 00 4E 00 4F 00 20 00 74 00 68 00 E0 00 6E 00   //  .N.O. .t.h...n.
                                      68 00 20 00 31 00 33 00 2D 00 31 00 34 00 2E 00 ) // h. .1.3.-.1.4...
      IL_0063:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_0068:  call       class [DTools]DTools.Core.ValidationResult [DTools]DTools.Core.CircuitRuleChecker::ValidateDolCircuit(class [DTools]DTools.Core.DolStarterConfig)
      IL_006d:  callvirt   instance class [mscorlib]System.Collections.Generic.List`1<string> [DTools]DTools.Core.ValidationResult::get_Corrections()
      IL_0072:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<string>::get_Count()
      IL_0077:  ldc.i4.0
      IL_0078:  cgt
      IL_007a:  ldstr      bytearray (43 00 68 00 BF 1E 20 00 11 01 D9 1E 20 00 43 00   // C.h... ..... .C.
                                      68 00 75 00 A9 1E 6E 00 20 00 6B 00 F9 1E 20 00   // h.u...n. .k... .
                                      74 00 68 00 75 00 AD 1E 74 00 20 00 70 00 68 00   // t.h.u...t. .p.h.
                                      A3 1E 69 00 20 00 67 00 68 00 69 00 20 00 6E 00   // ..i. .g.h.i. .n.
                                      68 00 AD 1E 6E 00 20 00 73 00 ED 1E 61 00 20 00   // h...n. .s...a. .
                                      63 00 68 00 EF 1E 61 00 2E 00 )                   // c.h...a...
      IL_007f:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_0084:  ret
    } // end of method '<>c'::'<Main>b__0_2'

    .method assembly hidebysig instance void 
            '<Main>b__0_3'() cil managed
    {
      // Code size       82 (0x52)
      .maxstack  3
      IL_0000:  newobj     instance void [DTools]DTools.Core.StarDeltaStarterConfig::.ctor()
      IL_0005:  dup
      IL_0006:  ldc.i4.1
      IL_0007:  callvirt   instance void [DTools]DTools.Core.StarDeltaStarterConfig::set_Mode(valuetype [DTools]DTools.Core.StarterMode)
      IL_000c:  dup
      IL_000d:  ldc.i4.0
      IL_000e:  callvirt   instance void [DTools]DTools.Core.StarDeltaStarterConfig::set_EnableCrossElectricalInterlock(bool)
      IL_0013:  call       class [DTools]DTools.Core.ValidationResult [DTools]DTools.Core.CircuitRuleChecker::ValidateStarDeltaCircuit(class [DTools]DTools.Core.StarDeltaStarterConfig)
      IL_0018:  callvirt   instance bool [DTools]DTools.Core.ValidationResult::get_IsValid()
      IL_001d:  ldc.i4.0
      IL_001e:  ceq
      IL_0020:  ldstr      bytearray (50 00 68 00 A3 1E 69 00 20 00 62 00 E1 00 6F 00   // P.h...i. .b...o.
                                      20 00 6C 00 D7 1E 69 00 20 00 6E 00 67 00 68 00   //  .l...i. .n.g.h.
                                      69 00 EA 00 6D 00 20 00 74 00 72 00 CD 1E 6E 00   // i...m. .t.r...n.
                                      67 00 20 00 6B 00 68 00 69 00 20 00 74 00 68 00   // g. .k.h.i. .t.h.
                                      69 00 BF 1E 75 00 20 00 6C 00 69 00 EA 00 6E 00   // i...u. .l.i...n.
                                      20 00 11 01 D9 1E 6E 00 67 00 20 00 11 01 69 00   //  .....n.g. ...i.
                                      C7 1E 6E 00 20 00 63 00 68 00 E9 00 6F 00 2E 00 ) // ..n. .c.h...o...
      IL_0025:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_002a:  newobj     instance void [DTools]DTools.Core.StarDeltaStarterConfig::.ctor()
      IL_002f:  dup
      IL_0030:  ldc.i4.1
      IL_0031:  callvirt   instance void [DTools]DTools.Core.StarDeltaStarterConfig::set_Mode(valuetype [DTools]DTools.Core.StarterMode)
      IL_0036:  dup
      IL_0037:  ldc.i4.1
      IL_0038:  callvirt   instance void [DTools]DTools.Core.StarDeltaStarterConfig::set_EnableCrossElectricalInterlock(bool)
      IL_003d:  call       class [DTools]DTools.Core.ValidationResult [DTools]DTools.Core.CircuitRuleChecker::ValidateStarDeltaCircuit(class [DTools]DTools.Core.StarDeltaStarterConfig)
      IL_0042:  callvirt   instance bool [DTools]DTools.Core.ValidationResult::get_IsValid()
      IL_0047:  ldstr      bytearray (4D 00 A1 1E 63 00 68 00 20 00 70 00 68 00 A3 1E   // M...c.h. .p.h...
                                      69 00 20 00 68 00 E3 1E 70 00 20 00 6C 00 C7 1E   // i. .h...p. .l...
                                      20 00 6B 00 68 00 69 00 20 00 63 00 F3 00 20 00   //  .k.h.i. .c... .
                                      11 01 E7 1E 20 00 6C 00 69 00 EA 00 6E 00 20 00   // .... .l.i...n. .
                                      11 01 D9 1E 6E 00 67 00 20 00 63 00 68 00 E9 00   // ....n.g. .c.h...
                                      6F 00 2E 00 )                                     // o...
      IL_004c:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_0051:  ret
    } // end of method '<>c'::'<Main>b__0_3'

    .method assembly hidebysig instance void 
            '<Main>b__0_4'() cil managed
    {
      // Code size       67 (0x43)
      .maxstack  3
      IL_0000:  ldc.i4.0
      IL_0001:  call       class [mscorlib]System.Collections.Generic.List`1<string> [DTools]DTools.Core.CircuitRuleChecker::SimulateStarDeltaFSM(bool)
      IL_0006:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<string>::get_Count()
      IL_000b:  ldc.i4.5
      IL_000c:  clt
      IL_000e:  ldc.i4.0
      IL_000f:  ceq
      IL_0011:  ldstr      bytearray (43 00 68 00 75 00 20 00 74 00 72 00 EC 00 6E 00   // C.h.u. .t.r...n.
                                      68 00 20 00 62 00 EC 00 6E 00 68 00 20 00 74 00   // h. .b...n.h. .t.
                                      68 00 B0 01 DD 1E 6E 00 67 00 20 00 70 00 68 00   // h.....n.g. .p.h.
                                      A3 1E 69 00 20 00 63 00 F3 00 20 00 ED 00 74 00   // ..i. .c... ...t.
                                      20 00 6E 00 68 00 A5 1E 74 00 20 00 35 00 20 00   //  .n.h...t. .5. .
                                      62 00 B0 01 DB 1E 63 00 2E 00 )                   // b.....c...
      IL_0016:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_001b:  ldc.i4.1
      IL_001c:  call       class [mscorlib]System.Collections.Generic.List`1<string> [DTools]DTools.Core.CircuitRuleChecker::SimulateStarDeltaFSM(bool)
      IL_0021:  dup
      IL_0022:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<string>::get_Count()
      IL_0027:  ldc.i4.1
      IL_0028:  sub
      IL_0029:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<string>::get_Item(int32)
      IL_002e:  ldstr      "TRIP"
      IL_0033:  callvirt   instance bool [mscorlib]System.String::Contains(string)
      IL_0038:  ldstr      bytearray (42 00 B0 01 DB 1E 63 00 20 00 63 00 75 00 D1 1E   // B.....c. .c.u...
                                      69 00 20 00 6B 00 68 00 69 00 20 00 74 00 72 00   // i. .k.h.i. .t.r.
                                      69 00 70 00 20 00 70 00 68 00 A3 1E 69 00 20 00   // i.p. .p.h...i. .
                                      6C 00 E0 00 20 00 72 00 A1 01 2D 00 6C 00 65 00   // l... .r...-.l.e.
                                      20 00 6E 00 68 00 69 00 C7 1E 74 00 20 00 6E 00   //  .n.h.i...t. .n.
                                      67 00 AF 1E 74 00 20 00 74 00 6F 00 E0 00 6E 00   // g...t. .t.o...n.
                                      20 00 62 00 D9 1E 20 00 6D 00 A1 1E 63 00 68 00   //  .b... .m...c.h.
                                      2E 00 )                                           // ..
      IL_003d:  call       void DTools.Tests.Program::Assert(bool,
                                                             string)
      IL_0042:  ret
    } // end of method '<>c'::'<Main>b__0_4'

  } // end of class '<>c'

  .method private hidebysig static int32 
          Main(string[] args) cil managed
  {
    .entrypoint
    // Code size       329 (0x149)
    .maxstack  4
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  call       class [mscorlib]System.Text.Encoding [mscorlib]System.Text.Encoding::get_UTF8()
    IL_0005:  call       void [mscorlib]System.Console::set_OutputEncoding(class [mscorlib]System.Text.Encoding)
    IL_000a:  ldstr      "=================================================="
    + "============="
    IL_000f:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0014:  ldstr      bytearray (20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   //  . . . . . . . .
                                    20 00 20 00 44 00 54 00 4F 00 4F 00 4C 00 53 00   //  . .D.T.O.O.L.S.
                                    20 00 50 00 52 00 4F 00 20 00 2D 00 20 00 42 00   //  .P.R.O. .-. .B.
                                    D8 1E 20 00 4B 00 49 00 C2 1E 4D 00 20 00 54 00   // .. .K.I...M. .T.
                                    48 00 EC 1E 20 00 54 00 F0 1E 20 00 10 01 D8 1E   // H... .T... .....
                                    4E 00 47 00 20 00 28 00 55 00 4E 00 49 00 54 00   // N.G. .(.U.N.I.T.
                                    20 00 54 00 45 00 53 00 54 00 53 00 29 00 20 00   //  .T.E.S.T.S.). .
                                    20 00 20 00 20 00 20 00 20 00 20 00 20 00 )       //  . . . . . . .
    IL_0019:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_001e:  ldstr      "=================================================="
    + "=============\n"
    IL_0023:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0028:  ldc.i4.0
    IL_0029:  stloc.0
    IL_002a:  ldc.i4.0
    IL_002b:  stloc.1
    IL_002c:  ldstr      bytearray (54 00 45 00 53 00 54 00 20 00 31 00 3A 00 20 00   // T.E.S.T. .1.:. .
                                    4B 00 69 00 C3 1E 6D 00 20 00 74 00 72 00 61 00   // K.i...m. .t.r.a.
                                    20 00 68 00 EC 00 6E 00 68 00 20 00 68 00 CD 1E   //  .h...n.h. .h...
                                    63 00 20 00 4D 00 B7 1E 74 00 20 00 62 00 B1 1E   // c. .M...t. .b...
                                    6E 00 67 00 20 00 43 00 68 00 69 00 BF 1E 75 00   // n.g. .C.h.i...u.
                                    20 00 73 00 E1 00 6E 00 67 00 20 00 31 00 32 00   //  .s...n.g. .1.2.
                                    78 00 36 00 6D 00 )                               // x.6.m.
    IL_0031:  ldsfld     class [mscorlib]System.Action DTools.Tests.Program/'<>c'::'<>9__0_0'
    IL_0036:  dup
    IL_0037:  brtrue.s   IL_0050

    IL_0039:  pop
    IL_003a:  ldsfld     class DTools.Tests.Program/'<>c' DTools.Tests.Program/'<>c'::'<>9'
    IL_003f:  ldftn      instance void DTools.Tests.Program/'<>c'::'<Main>b__0_0'()
    IL_0045:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                      native int)
    IL_004a:  dup
    IL_004b:  stsfld     class [mscorlib]System.Action DTools.Tests.Program/'<>c'::'<>9__0_0'
    IL_0050:  ldloca.s   V_0
    IL_0052:  ldloca.s   V_1
    IL_0054:  call       void DTools.Tests.Program::RunTest(string,
                                                            class [mscorlib]System.Action,
                                                            int32&,
                                                            int32&)
    IL_0059:  ldstr      bytearray (54 00 45 00 53 00 54 00 20 00 32 00 3A 00 20 00   // T.E.S.T. .2.:. .
                                    4B 00 69 00 C3 1E 6D 00 20 00 74 00 72 00 61 00   // K.i...m. .t.r.a.
                                    20 00 70 00 68 00 E1 00 74 00 20 00 68 00 69 00   //  .p.h...t. .h.i.
                                    C7 1E 6E 00 20 00 11 01 E8 00 6E 00 20 00 6E 00   // ..n. .....n. .n.
                                    B1 1E 6D 00 20 00 6E 00 67 00 6F 00 E0 00 69 00   // ..m. .n.g.o...i.
                                    20 00 70 00 68 00 F2 00 6E 00 67 00 20 00 68 00   //  .p.h...n.g. .h.
                                    6F 00 B7 1E 63 00 20 00 63 00 68 00 A1 1E 6D 00   // o...c. .c.h...m.
                                    20 00 74 00 B0 01 DD 1E 6E 00 67 00 )             //  .t.....n.g.
    IL_005e:  ldsfld     class [mscorlib]System.Action DTools.Tests.Program/'<>c'::'<>9__0_1'
    IL_0063:  dup
    IL_0064:  brtrue.s   IL_007d

    IL_0066:  pop
    IL_0067:  ldsfld     class DTools.Tests.Program/'<>c' DTools.Tests.Program/'<>c'::'<>9'
    IL_006c:  ldftn      instance void DTools.Tests.Program/'<>c'::'<Main>b__0_1'()
    IL_0072:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                      native int)
    IL_0077:  dup
    IL_0078:  stsfld     class [mscorlib]System.Action DTools.Tests.Program/'<>c'::'<>9__0_1'
    IL_007d:  ldloca.s   V_0
    IL_007f:  ldloca.s   V_1
    IL_0081:  call       void DTools.Tests.Program::RunTest(string,
                                                            class [mscorlib]System.Action,
                                                            int32&,
                                                            int32&)
    IL_0086:  ldstr      bytearray (54 00 45 00 53 00 54 00 20 00 33 00 3A 00 20 00   // T.E.S.T. .3.:. .
                                    4B 00 69 00 C3 1E 6D 00 20 00 74 00 72 00 61 00   // K.i...m. .t.r.a.
                                    20 00 6D 00 A1 1E 63 00 68 00 20 00 6B 00 68 00   //  .m...c.h. .k.h.
                                    DF 1E 69 00 20 00 11 01 D9 1E 6E 00 67 00 20 00   // ..i. .....n.g. .
                                    74 00 72 00 F1 1E 63 00 20 00 74 00 69 00 BF 1E   // t.r...c. .t.i...
                                    70 00 20 00 44 00 4F 00 4C 00 20 00 26 00 20 00   // p. .D.O.L. .&. .
                                    43 00 68 00 E2 00 6E 00 20 00 49 00 45 00 43 00 ) // C.h...n. .I.E.C.
    IL_008b:  ldsfld     class [mscorlib]System.Action DTools.Tests.Program/'<>c'::'<>9__0_2'
    IL_0090:  dup
    IL_0091:  brtrue.s   IL_00aa

    IL_0093:  pop
    IL_0094:  ldsfld     class DTools.Tests.Program/'<>c' DTools.Tests.Program/'<>c'::'<>9'
    IL_0099:  ldftn      instance void DTools.Tests.Program/'<>c'::'<Main>b__0_2'()
    IL_009f:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                      native int)
    IL_00a4:  dup
    IL_00a5:  stsfld     class [mscorlib]System.Action DTools.Tests.Program/'<>c'::'<>9__0_2'
    IL_00aa:  ldloca.s   V_0
    IL_00ac:  ldloca.s   V_1
    IL_00ae:  call       void DTools.Tests.Program::RunTest(string,
                                                            class [mscorlib]System.Action,
                                                            int32&,
                                                            int32&)
    IL_00b3:  ldstr      bytearray (54 00 45 00 53 00 54 00 20 00 34 00 3A 00 20 00   // T.E.S.T. .4.:. .
                                    4B 00 69 00 C3 1E 6D 00 20 00 74 00 72 00 61 00   // K.i...m. .t.r.a.
                                    20 00 6C 00 69 00 EA 00 6E 00 20 00 11 01 D9 1E   //  .l.i...n. .....
                                    6E 00 67 00 20 00 63 00 68 00 E9 00 6F 00 20 00   // n.g. .c.h...o. .
                                    53 00 61 00 6F 00 20 00 2D 00 20 00 54 00 61 00   // S.a.o. .-. .T.a.
                                    6D 00 20 00 47 00 69 00 E1 00 63 00 20 00 28 00   // m. .G.i...c. .(.
                                    43 00 68 00 D1 1E 6E 00 67 00 20 00 6E 00 67 00   // C.h...n.g. .n.g.
                                    AF 1E 6E 00 20 00 6D 00 A1 1E 63 00 68 00 29 00 ) // ..n. .m...c.h.).
    IL_00b8:  ldsfld     class [mscorlib]System.Action DTools.Tests.Program/'<>c'::'<>9__0_3'
    IL_00bd:  dup
    IL_00be:  brtrue.s   IL_00d7

    IL_00c0:  pop
    IL_00c1:  ldsfld     class DTools.Tests.Program/'<>c' DTools.Tests.Program/'<>c'::'<>9'
    IL_00c6:  ldftn      instance void DTools.Tests.Program/'<>c'::'<Main>b__0_3'()
    IL_00cc:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                      native int)
    IL_00d1:  dup
    IL_00d2:  stsfld     class [mscorlib]System.Action DTools.Tests.Program/'<>c'::'<>9__0_3'
    IL_00d7:  ldloca.s   V_0
    IL_00d9:  ldloca.s   V_1
    IL_00db:  call       void DTools.Tests.Program::RunTest(string,
                                                            class [mscorlib]System.Action,
                                                            int32&,
                                                            int32&)
    IL_00e0:  ldstr      bytearray (54 00 45 00 53 00 54 00 20 00 35 00 3A 00 20 00   // T.E.S.T. .5.:. .
                                    4D 00 F4 00 20 00 70 00 68 00 CF 1E 6E 00 67 00   // M... .p.h...n.g.
                                    20 00 46 00 53 00 4D 00 20 00 63 00 68 00 75 00   //  .F.S.M. .c.h.u.
                                    20 00 74 00 72 00 EC 00 6E 00 68 00 20 00 53 00   //  .t.r...n.h. .S.
                                    74 00 61 00 72 00 2D 00 44 00 65 00 6C 00 74 00   // t.a.r.-.D.e.l.t.
                                    61 00 3A 00 20 00 4F 00 46 00 46 00 20 00 2D 00   // a.:. .O.F.F. .-.
                                    3E 00 20 00 53 00 54 00 41 00 52 00 54 00 20 00   // >. .S.T.A.R.T. .
                                    2D 00 3E 00 20 00 53 00 54 00 41 00 52 00 20 00   // -.>. .S.T.A.R. .
                                    2D 00 3E 00 20 00 44 00 45 00 41 00 44 00 5F 00   // -.>. .D.E.A.D._.
                                    54 00 49 00 4D 00 45 00 20 00 2D 00 3E 00 20 00   // T.I.M.E. .-.>. .
                                    44 00 45 00 4C 00 54 00 41 00 20 00 2D 00 3E 00   // D.E.L.T.A. .-.>.
                                    20 00 54 00 52 00 49 00 50 00 )                   //  .T.R.I.P.
    IL_00e5:  ldsfld     class [mscorlib]System.Action DTools.Tests.Program/'<>c'::'<>9__0_4'
    IL_00ea:  dup
    IL_00eb:  brtrue.s   IL_0104

    IL_00ed:  pop
    IL_00ee:  ldsfld     class DTools.Tests.Program/'<>c' DTools.Tests.Program/'<>c'::'<>9'
    IL_00f3:  ldftn      instance void DTools.Tests.Program/'<>c'::'<Main>b__0_4'()
    IL_00f9:  newobj     instance void [mscorlib]System.Action::.ctor(object,
                                                                      native int)
    IL_00fe:  dup
    IL_00ff:  stsfld     class [mscorlib]System.Action DTools.Tests.Program/'<>c'::'<>9__0_4'
    IL_0104:  ldloca.s   V_0
    IL_0106:  ldloca.s   V_1
    IL_0108:  call       void DTools.Tests.Program::RunTest(string,
                                                            class [mscorlib]System.Action,
                                                            int32&,
                                                            int32&)
    IL_010d:  ldstr      "\n-------------------------------------------------"
    + "--------------"
    IL_0112:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0117:  ldstr      bytearray (4B 00 BE 1E 54 00 20 00 51 00 55 00 A2 1E 3A 00   // K...T. .Q.U...:.
                                    20 00 10 01 E3 00 20 00 63 00 68 00 A1 1E 79 00   //  ..... .c.h...y.
                                    20 00 7B 00 30 00 7D 00 20 00 62 00 E0 00 69 00   //  .{.0.}. .b...i.
                                    20 00 74 00 65 00 73 00 74 00 2E 00 20 00 54 00   //  .t.e.s.t... .T.
                                    68 00 E0 00 6E 00 68 00 20 00 63 00 F4 00 6E 00   // h...n.h. .c...n.
                                    67 00 3A 00 20 00 7B 00 31 00 7D 00 2C 00 20 00   // g.:. .{.1.}.,. .
                                    54 00 68 00 A5 1E 74 00 20 00 62 00 A1 1E 69 00   // T.h...t. .b...i.
                                    3A 00 20 00 7B 00 32 00 7D 00 )                   // :. .{.2.}.
    IL_011c:  ldloc.0
    IL_011d:  ldloc.1
    IL_011e:  add
    IL_011f:  box        [mscorlib]System.Int32
    IL_0124:  ldloc.0
    IL_0125:  box        [mscorlib]System.Int32
    IL_012a:  ldloc.1
    IL_012b:  box        [mscorlib]System.Int32
    IL_0130:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object,
                                                                object)
    IL_0135:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_013a:  ldstr      "--------------------------------------------------"
    + "-------------"
    IL_013f:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0144:  ldloc.1
    IL_0145:  ldc.i4.0
    IL_0146:  cgt.un
    IL_0148:  ret
  } // end of method Program::Main

  .method private hidebysig static void  RunTest(string name,
                                                 class [mscorlib]System.Action testAction,
                                                 int32& passed,
                                                 int32& failed) cil managed
  {
    // Code size       91 (0x5b)
    .maxstack  4
    .locals init (class [mscorlib]System.Exception V_0)
    .try
    {
      IL_0000:  ldarg.1
      IL_0001:  callvirt   instance void [mscorlib]System.Action::Invoke()
      IL_0006:  ldc.i4.s   10
      IL_0008:  call       void [mscorlib]System.Console::set_ForegroundColor(valuetype [mscorlib]System.ConsoleColor)
      IL_000d:  ldstr      "[PASS] "
      IL_0012:  ldarg.0
      IL_0013:  call       string [mscorlib]System.String::Concat(string,
                                                                  string)
      IL_0018:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_001d:  call       void [mscorlib]System.Console::ResetColor()
      IL_0022:  ldarg.2
      IL_0023:  ldarg.2
      IL_0024:  ldind.i4
      IL_0025:  ldc.i4.1
      IL_0026:  add
      IL_0027:  stind.i4
      IL_0028:  leave.s    IL_005a

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_002a:  stloc.0
      IL_002b:  ldc.i4.s   12
      IL_002d:  call       void [mscorlib]System.Console::set_ForegroundColor(valuetype [mscorlib]System.ConsoleColor)
      IL_0032:  ldstr      "[FAIL] "
      IL_0037:  ldarg.0
      IL_0038:  ldstr      ": "
      IL_003d:  ldloc.0
      IL_003e:  callvirt   instance string [mscorlib]System.Exception::get_Message()
      IL_0043:  call       string [mscorlib]System.String::Concat(string,
                                                                  string,
                                                                  string,
                                                                  string)
      IL_0048:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_004d:  call       void [mscorlib]System.Console::ResetColor()
      IL_0052:  ldarg.3
      IL_0053:  ldarg.3
      IL_0054:  ldind.i4
      IL_0055:  ldc.i4.1
      IL_0056:  add
      IL_0057:  stind.i4
      IL_0058:  leave.s    IL_005a

    }  // end handler
    IL_005a:  ret
  } // end of method Program::RunTest

  .method private hidebysig static void  Assert(bool condition,
                                                string message) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0014

    IL_0003:  ldstr      bytearray (4B 00 68 00 B3 1E 6E 00 67 00 20 00 11 01 CB 1E   // K.h...n.g. .....
                                    6E 00 68 00 20 00 74 00 68 00 A5 1E 74 00 20 00   // n.h. .t.h...t. .
                                    62 00 A1 1E 69 00 3A 00 20 00 )                   // b...i.:. .
    IL_0008:  ldarg.1
    IL_0009:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_000e:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
    IL_0013:  throw

    IL_0014:  ret
  } // end of method Program::Assert

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

} // end of class DTools.Tests.Program


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file C:\Users\Duong\.gemini\antigravity-ide\scratch\DTT_AutoCAD_Plugin\dist\tests_decompiled.res
