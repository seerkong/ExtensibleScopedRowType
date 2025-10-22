using Kon.Core;
using Kon.Core.Converter;
using Kon.Core.Node;
using Xunit;

namespace Kon.Core.Tests;

public class KonParserTests
{
  private static void AssertRoundTrip(string source)
  {
    var node = KonParser.Parse(source);
    var singleLine1 = KonFormater.SingleLine(node);
    var reparsedSingle = KonParser.Parse(singleLine1);
    var singleLine2 = KonFormater.SingleLine(reparsedSingle);
    var prettified = KonFormater.Prettify(node);
    var reparsedPretty = KonParser.Parse(prettified);
    var singleLine3 = KonFormater.SingleLine(reparsedPretty);


    Assert.Equal(singleLine1, singleLine2);
    Assert.Equal(singleLine1, singleLine3);
  }

  [Fact]
  public void ParseInOutTable1()
  {
    const string source = """
        (fn :|string int|<int string>)
        """;
    AssertRoundTrip(source);
  }

  [Fact]
  public void ParseInOutTable2()
  {
    const string source = """
        (fn :|string int -- !int a|<int string>)
        """;
    AssertRoundTrip(source);
  }

  [Fact]
  public void ParseInOutTable3()
  {
    const string source = """
        (fn :|string int -> int|<int string>)
        """;
    AssertRoundTrip(source);
  }

  [Fact]
  public void ParsePostfixCall()
  {
    const string source = """
        (arg1 arg2 :bar)
        //(arg1 :bar arg2)
        //(arg1 :bar arg2;)
        """;
    AssertRoundTrip(source);
  }

  [Fact]
  public void MethodCall()
  {
    string sample = "(a :add 1;~+ 1;~round)";
    // string sample = "(person~laughWithVolume 20;)";
    var parsed = KonParser.Parse(sample);
    var formattedMethod = KonFormater.SingleLine(parsed);
    Assert.Equal(sample, formattedMethod);
  }

  [Fact]
  public void PropertyChain()
  {
    string chainSample = "(a~b~c~d)";
    var parsed = KonParser.Parse(chainSample);
    var formattedMethod = KonFormater.SingleLine(parsed);
    Assert.Equal(chainSample, formattedMethod);
  }


  [Fact]
  public void MethodCall_NoArgs()
  {
    const string source = """
        (1 ~to_float)
        """;
    AssertRoundTrip(source);
  }

  [Fact]
  public void StaticIndexChain()
  {
    string chainSample = "(a.:b.:c)";
    var parsed = KonParser.Parse(chainSample);
    var formattedMethod = KonFormater.SingleLine(parsed);
    Assert.Equal(chainSample, formattedMethod);
  }

  [Fact]
  public void ContainerIndexChain()
  {
    string chainSample = "(a::1::\"a\")";
    var parsed = KonParser.Parse(chainSample);
    var formattedMethod = KonFormater.SingleLine(parsed);
    Assert.Equal(chainSample, formattedMethod);
  }

  [Fact]
  public void CallChain()
  {
    var sampleChain = "(getPlusOneFn:|| :|1| 1 :add 1 1; :add 1;~+ 1;~to_float)";
    var parsedSampleChain = KonParser.Parse(sampleChain);
    var formattedChain = KonFormater.SingleLine(parsedSampleChain);
    Assert.Equal("(getPlusOneFn:|| :|1| 1 :add 1 1; :add 1;~+ 1;~to_float)", formattedChain);
  }

  [Fact]
  public void ChainUseAsDsl()
  {
    const string source = """
        // 表示使用Kon表示偏重数据的dsl的示例
        @(prefix1 (prefix2) [prefix3] {prefix4:1})
        @{a: 1 b: 2}
        @[x y]
        (@(tag_prefix1 tag_prefix2) mytagcore %(a b) #sys.idName
        @true_val_attr1
        @attr1: []
        @attr2: {}
        %{conf1:1 nested: {deep:2}}
        %namedconf1: {m:1}
        %namedconf2: {n:2}
        %[bodyitem1 bodyitem2]
        %branch1: [1] %branch2: [2]
        %slot1: (a %[b]) %slot2: (c %[d] e f)
        )
        %(postfix1 (postfix2) [postfix3] {postfix4:1})
        """;
    AssertRoundTrip(source);
  }


  [Fact]
  public void ChainUseAsScript()
  {
    const string source = """
        // 表示使用Kon表示代码逻辑的示例
        // 解释器本质是一个 Stack-oriented programming language
        // 操作数栈用于存储表达式中多个元素求值后的结果
        !(fn |int string -> string int|<T1 T2>) // 可以在KnChain前面用手动用! 前缀进行类型标记。也可以用于未来自动分析类型添加类型标记
        (fn #foo :|!int arg1 !string arg2 -> string int|<T1 T2> // 也可以在声明函数时标记类型
          %[
            // 用 func_name :|arg1 arg2| 表示 函数前缀调用
            // 在执行前缀表达式函数调用时，对KnChain依次求值，先把bar放进操作数栈，然后遍历到KnChain的下个节点，发现是一个前缀函数调用:<> 时，依次求值参数，然后使用操作数栈栈顶的作为函数，后面的作为参数，进行一次函数调用
            (bar :|arg1 arg2|)
            // 上述例子，等同于如下三种 栈操作表达式 的形式
            // 栈操作表达式，允许函数名后面再有参数，直到遇到 ) 或者;
            // 执行时，会先对参数求值，再放到操作数栈，然后调用函数
            (arg1 arg2 :bar)
            (arg1 :bar arg2)
            (arg1 :bar arg2;)
            // 对象方法调用表达式，表示对对象实例的方法调用
            (person ~laughWithVolume 20)
            // 函数前缀调用、栈操作表达式、对象方法调用表达式，可以在一个KnChain中链式执行
            // 例如下面例子, 应当返回 7.0
            //(getPlusOneFn:|| :|1| 1 :add 1 1; :add 1; ~+ 1; ~to_float)
            (getPlusOneFn:|| :|1| 1 :add 1 1; :add 1; ~+ 1 ;)
            // 在Kon中，使用 `symbol 表示符号
            // 例如 可以先向操作数栈中添加一个符号，然后主动一个send函数，发起对象调用
            (1 `biggerThan? 5 :send)
            // 在Kon中，也是使用`前缀，表示类似lisp/scheme的quasi-quote,
            // 使用,表示unquote,
            // 使用,@ 表示expand-to-sequence, 将一个值展开到一个array或者list中。对应isp的unquote-splicing
            // 使用,% 表示expand-to-map
            `(1 ,x ,@[1 2 3] {k1:1 k2:2 ,%{k3: 3 k4: 4}})
            // 如果想要返回多个值，可以使用 || 包裹多个表达式。这个结果将会放到操作数栈中
            |arg2 arg1|
          ]
        )
        """;
    AssertRoundTrip(source);

  }

}
