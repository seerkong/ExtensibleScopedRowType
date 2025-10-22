# Kon.Interpreter 解释器架构原理

本文档总结 Kon.Interpreter 的核心架构和执行机制，用于理解解释器的工作原理，为后续实现 InstanceCall 集成奠定基础。

## 目录

- [核心架构](#核心架构)
- [双栈模型](#双栈模型)
- [指令执行流程](#指令执行流程)
- [扩展注册表](#扩展注册表)
- [OpCode 系统](#opcode-系统)
- [环境管理](#环境管理)
- [Fiber 系统](#fiber-系统)
- [调用类型处理](#调用类型处理)
- [函数应用机制](#函数应用机制)
- [执行流程示例](#执行流程示例)

---

## 核心架构

Kon.Interpreter 是一个基于**双栈模型**的栈式虚拟机，支持：
- 词法作用域（Lexical Scoping）
- 闭包（Closures）
- 协作式多任务（Cooperative Multitasking via Fibers）
- 效果处理（Effect Handling via Continuations）
- 可扩展的指令系统

### 关键组件

```
KonInterpreterRuntime (运行时状态)
├── EnvTree (环境树 - 词法作用域)
├── FiberManager (Fiber 调度器 - 协作式多任务)
├── ExtensionRegistry (扩展注册表 - 指令处理器)
└── InstructionHistory (执行历史 - 调试跟踪)

每个 Fiber 包含：
├── OperandStack (操作数栈 - 值存储)
├── InstructionStack (指令栈 - 待执行指令)
└── CurrentEnvId (当前环境 ID)
```

**代码位置**:
- `src/Kon.Interpreter/Runtime/KonInterpreterRuntime.cs`

---

## 双栈模型

### 1. StackMachine<T> - 通用栈机制

**代码位置**: `src/Kon.Interpreter/Runtime/StackMachine.cs`

```csharp
public class StackMachine<T>
{
    public List<T> Items;              // 实际栈数据
    public int StackTop;                // 栈顶索引
    public List<int> FrameBottomIdxStack; // 栈帧底部索引栈
}
```

**栈帧（Frame）概念**：
- 栈被分割成多个栈帧
- 每个栈帧对应一个独立的作用域（如函数调用、表达式求值）
- 栈帧通过 `FrameBottomIdxStack` 管理嵌套关系

**关键操作**：
- `PushFrame()` - 创建新栈帧
- `PopFrameAllValues()` - 弹出整个栈帧的所有值
- `PopFrameAndPushTopVal()` - 弹出栈帧，但保留顶部值
- `PeekTop()` - 查看栈顶值（不弹出）

### 2. OperandStack - 操作数栈

**代码位置**: `src/Kon.Interpreter/Runtime/OperandStack.cs`

```csharp
public class OperandStack : StackMachine<KnNode>
```

- 存储**运行时值**（所有 KnNode 类型）
- 函数调用时参数和返回值都在这里
- 每个表达式求值都会创建新栈帧

**典型用法**：
```
Frame 0: [arg1, arg2, func_object, ]  ← 函数调用，用于Ctrl_ApplyToFrameTop
Frame 1: [func_object, arg1, arg2]  ← 函数调用，用于Ctrl_ApplyToFrameBottom
Frame 2: [intermediate_value]       ← 内部表达式
```

### 3. InstructionStack - 指令栈

**代码位置**: `src/Kon.Interpreter/Runtime/InstructionStack.cs`

```csharp
public class InstructionStack : StackMachine<Instruction>
```

- 存储**待执行的指令**
- 指令按照后进先出（LIFO）顺序执行
- 支持嵌套指令批次

**Instruction 结构**：
```csharp
public class Instruction
{
    public string OpCode;    // 操作码（如 "ValStack_PushValue"）
    public int EnvId;        // 执行时的环境 ID
    public object? Memo;     // 附加数据（可以是 KnNode、int、string 等）
    public string Comment;   // 调试注释
}
```

**代码位置**: `src/Kon.Interpreter/Runtime/Instruction.cs`

---

## 指令执行流程

### KonInterpreterEngine - 主执行循环

**代码位置**: `src/Kon.Interpreter/KonInterpreterEngine.cs`

```csharp
public static KnNode StartLoopSync(KonInterpreterRuntime runtime)
{
    var instruction = runtime.GetCurrentFiber().InstructionStack.PopValue();

    while (instruction.OpCode != KonOpCode.OpStack_LandSuccess &&
           instruction.OpCode != KonOpCode.OpStack_LandFail)
    {
        // 1. 从注册表获取处理器
        var handler = runtime.ExtensionRegistry.GetInstructionHandler(instruction.OpCode);

        // 2. 执行处理器
        handler(runtime, instruction);

        // 3. 记录执行历史（调试用）
        runtime.InstructionHistory.Add(log);

        // 4. 获取下一个可运行的 Fiber
        var nextRunFiber = runtime.FiberMgr.GetNextActiveFiber();

        // 5. 弹出下一条指令
        instruction = currentFiber.InstructionStack.PopValue();
    }

    // 返回操作数栈底部的结果
    return currentFiber.OperandStack.PeekBottomOfAllFrames();
}
```

### 执行循环的关键点

1. **指令驱动**：所有执行都由指令驱动，没有直接的递归解释
2. **处理器分派**：通过 OpCode 查找对应的处理器函数
3. **Fiber 调度**：支持多 Fiber 协作式调度
4. **终止条件**：`OpStack_LandSuccess` 或 `OpStack_LandFail`

---

## 扩展注册表

### ExtensionRegistry - 可扩展的处理器系统

**代码位置**: `src/Kon.Interpreter/Runtime/ExtensionRegistry.cs`

```csharp
public class ExtensionRegistry
{
    // 指令处理器: OpCode → Handler
    Dictionary<string, Action<KonInterpreterRuntime, Instruction>> InstructionHandlerMap;

    // 前缀关键字展开器: Keyword → Expander
    Dictionary<string, Action<KonInterpreterRuntime, KnChainNode>> PrefixKeywordExpanderMap;

    // 中缀关键字展开器: Keyword → Expander
    Dictionary<string, Action<KonInterpreterRuntime, KnChainNode>> InfixKeywordExpanderMap;
}
```

### ExtensionRegistryInitializer - 初始化器

**代码位置**: `src/Kon.Interpreter/Capsule/ExtensionRegistryInitializer.cs`

在运行时初始化时注册：
- **~25 个指令处理器**（OpCode handlers）
- **~8 个前缀关键字**（fn, var, set, if, cond, foreach, for, ++, try, perform）
- **1 个中缀关键字**（set_to）
- **标准库函数**（内置函数）

**注册示例**：
```csharp
registry.RegisterInstructionHandler(
    KonOpCode.ValStack_PushValue,
    ValueStackHandler.RunPushValue
);

registry.RegisterPrefixKeyword(
    "fn",
    FuncHandler.ExpandFunctionDefinition
);
```

---

## OpCode 系统

### KonOpCode - 字符串化操作码

**代码位置**: `src/Kon.Interpreter/Runtime/KonOpCode.cs`

使用字符串而非枚举，便于动态扩展。按功能分类：

#### 1. 操作栈控制
```csharp
OpStack_LandSuccess  // 执行成功标记
OpStack_LandFail     // 执行失败标记
```

#### 2. 值栈操作
```csharp
ValStack_PushFrame             // 创建新栈帧
ValStack_PopValue              // 弹出单个值
ValStack_PushValue             // 压入单个值
ValStack_Duplicate             // 复制栈顶值
ValStack_PopFrameAndPushTopVal // 弹出栈帧，保留顶值
ValStack_PopFrameIgnoreResult  // 弹出整个栈帧
```

#### 3. 环境操作
```csharp
Env_DiveProcessEnv   // 创建 Process 级子环境
Env_DiveLocalEnv     // 创建 Local 级子环境
Env_Rise             // 返回父环境
Env_DeclareLocalVar  // 声明局部变量
Env_DeclareGlobalVar // 声明全局变量
Env_SetLocalEnv      // 设置局部环境变量
Env_SetGlobalEnv     // 设置全局环境变量
```

#### 4. 控制流操作
```csharp
Ctrl_ApplyToFrameBottom  // 函数调用（函数在栈帧底部）
Ctrl_ApplyToFrameTop     // 函数调用（函数在栈帧顶部）
Ctrl_Jump                // 无条件跳转
Ctrl_JumpIfFalse         // 条件跳转
Ctrl_IterConditionPairs  // 迭代条件对（cond 语句）
Ctrl_IterForEachLoop     // foreach 循环
Ctrl_IterForLoop         // for 循环
```

#### 5. 节点操作
```csharp
Node_RunNode           // 执行单个 KnNode
Node_RunBlock          // 执行代码块
Node_IterEvalChainNode // 求值链式表达式
Node_MakeArray         // 构造数组
Node_MakeMap           // 构造字典
Node_RunGetProperty    // 获取属性（目前未实现）
```

#### 6. Fiber 操作
```csharp
Ctrl_CurrentFiberToIdle      // 当前 Fiber 转为空闲
Ctrl_CurrentFiberToSuspended // 当前 Fiber 转为挂起
Ctrl_AwakenMultiFibers       // 唤醒多个 Fiber
Ctrl_FinalizeFiber           // 结束 Fiber
```

---

## 环境管理

### Env - 单个环境/作用域

**代码位置**: `src/Kon.Interpreter/Runtime/Env.cs`

```csharp
public class Env
{
    public int Id;                      // 唯一 ID
    public EnvType Type;                // 环境类型
    public string Name;                 // 环境名称
    public Dictionary<string, KnNode> VarDict; // 变量字典
    public Env? Parent;                 // 词法父环境
}

public enum EnvType
{
    BuiltIn,   // 内置环境（根）
    Global,    // 全局环境
    Process,   // 进程级环境（函数定义域）
    Local      // 局部环境（代码块）
}
```

**环境创建**：
- `CreateBuiltInEnv()` - 创建根环境
- `CreateGlobalEnv(parent)` - 创建全局环境
- `CreateLexicalScope(parent, type, name)` - 创建词法子环境

### EnvTree - 环境树

**代码位置**: `src/Kon.Interpreter/Runtime/EnvTree.cs`

```csharp
public class EnvTree : SingleEntryGraph<Env, int>
```

- 继承自有向无环图（DAG）结构
- 管理环境的层次关系
- 支持词法作用域查找

**关键方法**：
- `LookupDeclareEnv(fromEnv, key)` - 沿词法父链查找变量声明位置
- `CreateLexicalScope(parentEnv, type, name)` - 创建子环境并建立父子关系

**环境层次结构**：
```
BuiltIn (根环境 - 内置函数)
  └─ Global (全局环境 - 全局变量)
      ├─ Process (函数定义环境)
      │   └─ Local (函数内部代码块)
      └─ Local (全局代码块)
```

---

## Fiber 系统

### Fiber - 轻量级执行线程

**代码位置**: `src/Kon.Interpreter/Runtime/Fiber.cs`

```csharp
public class Fiber
{
    public int Id;                      // 唯一 ID
    private FiberState _state;          // 当前状态
    public int CurrentEnvId;            // 当前环境 ID
    public OperandStack OperandStack;   // 操作数栈
    public InstructionStack InstructionStack; // 指令栈
    public Fiber? ParentFiber;          // 父 Fiber
}

public enum FiberState
{
    Runnable,   // 可运行
    Running,    // 正在运行
    Idle,       // 空闲（等待输入）
    Suspended,  // 挂起（等待唤醒）
    Dead        // 已结束
}
```

**Fiber 创建**：
- `CreateRootFiber()` - 创建主 Fiber
- `CreateSubFiber(parentFiber, initState)` - 创建子 Fiber

### FiberManager - Fiber 调度器

**代码位置**: `src/Kon.Interpreter/Runtime/FiberManager.cs`

```csharp
public class FiberManager
{
    private List<Fiber> _runnableFibers;   // 可运行 Fiber 列表（索引 0 为当前运行）
    private List<Fiber> _idleFibers;       // 空闲 Fiber 列表
    private List<Fiber> _suspendedFibers;  // 挂起 Fiber 列表
    private Queue<ResumeFiberToken> _resumeEventQueue; // 恢复事件队列
}
```

**调度逻辑** (`GetNextActiveFiber()`):
1. 如果只有根 Fiber → 继续运行根 Fiber
2. 如果有非根 Fiber → 优先调度非根 Fiber
3. 挂起/空闲的 Fiber 可通过 Resume 事件唤醒

**Resume 机制**：
```csharp
public class ResumeFiberToken
{
    public int FiberId;              // 要恢复的 Fiber ID
    public List<KnNode> ResultList;  // 恢复时传入的值
}
```

- Fiber 可以挂起等待外部事件
- 通过 `AddResumeEvent()` 添加恢复事件
- `WaitAndConsumeResumeTokenSync()` 处理恢复事件

---

## 调用类型处理

### ChainExprHandler - 链式表达式处理器

**代码位置**: `src/Kon.Interpreter/Handlers/Call/ChainExprHandler.cs`

处理 `KnChainNode` 的求值，支持三种主要调用类型：

### 1. PrefixCall - 前缀调用

**语法**: `(func arg1 arg2)`

**指令展开**：
```csharp
if (chainNode.CallType == KnChainNode.PrefixCall)
{
    runtime.AddOp(KonOpCode.Node_RunNode, chainNode.Core);  // 压入函数

    for (var i = 0; i < chainNode.CallParams.Size(); i++)
    {
        runtime.AddOp(KonOpCode.Node_RunNode, chainNode.CallParams[i]); // 压入参数
    }

    runtime.AddOp(KonOpCode.Ctrl_ApplyToFrameBottom); // 应用（函数在底部）
}
```

**栈状态**：
```
Before Apply: [func, arg1, arg2]  ← Frame
After Apply:  [result]
```

### 2. PostfixCall - 后缀调用

**语法**: `(arg1 arg2 :func)`

**指令展开**：
```csharp
if (chainNode.CallType == KnChainNode.PostfixCall)
{
    for (var i = 0; i < chainNode.CallParams.Size(); i++)
    {
        runtime.AddOp(KonOpCode.Node_RunNode, chainNode.CallParams[i]); // 压入参数
    }

    runtime.AddOp(KonOpCode.Node_RunNode, chainNode.Core); // 压入函数
    runtime.AddOp(KonOpCode.Ctrl_ApplyToFrameTop);  // 应用（函数在顶部）
}
```

**栈状态**：
```
Before Apply: [arg1, arg2, func]  ← Frame
After Apply:  [result]
```

### 3. InstanceCall - 实例调用

**语法**: `(obj~method arg1 arg2)` （使用 `~` 分隔符）

**指令展开**（当前实现）：
```csharp
if (chainNode.CallType == KnChainNode.InstanceCall)
{
    if (chainNode.CallParams != null)
    {
        runtime.AddOp(KonOpCode.ValStack_PushFrame);
        runtime.AddOp(KonOpCode.Node_RunGetProperty); // ⚠️ 未实现

        for (var i = 0; i < chainNode.CallParams.Size(); i++)
        {
            runtime.AddOp(KonOpCode.Node_RunNode, chainNode.CallParams[i]);
        }

        runtime.AddOp(KonOpCode.Ctrl_ApplyToFrameBottom);
        runtime.AddOp(KonOpCode.ValStack_PopFrameAndPushTopVal);
    }
    else
    {
        runtime.AddOp(KonOpCode.Node_RunGetProperty); // ⚠️ 未实现
    }
}
```

**⚠️ 当前状态**：`Node_RunGetProperty` OpCode **尚未实现**，这是我们需要添加的部分！

---

## 函数应用机制

### FuncHandler - 函数调用处理器

**代码位置**: `src/Kon.Interpreter/Handlers/PrefixKeyword/FuncHandler.cs`

### Ctrl_ApplyToFrameBottom

```csharp
public static void RunApplyToFrameBottom(KonInterpreterRuntime runtime, Instruction instruction)
{
    var values = runtime.GetCurrentFiber().OperandStack.PopFrameAllValues();

    var func = values[0];              // 第一个值是函数
    var args = values.Skip(1).ToList(); // 其余是参数

    RunApplyToFunc(runtime, func, args);
}
```

### Ctrl_ApplyToFrameTop

```csharp
public static void RunApplyToFrameTop(KonInterpreterRuntime runtime, Instruction instruction)
{
    var func = runtime.GetCurrentFiber().OperandStack.PopValue(); // 弹出函数
    var args = runtime.GetCurrentFiber().OperandStack.PeekAndClearFrameAllValues(); // 获取参数

    RunApplyToFunc(runtime, func, args);
}
```

### RunApplyToFunc - 三种函数类型

#### 1. Lambda 函数 (KnLambdaFunction)

```csharp
// 1. 创建函数的局部环境
runtime.AddOp(KonOpCode.Env_DiveLocalEnv);

// 2. 绑定参数到环境
for (int i = 0; i < paramNames.Count; i++)
{
    runtime.AddOp(KonOpCode.Env_DeclareLocalVar, paramNames[i]);
    runtime.AddOp(KonOpCode.ValStack_PushValue, args[i]);
}

// 3. 执行函数体
runtime.AddOp(KonOpCode.Node_RunBlock, lambda.Body);

// 4. 获取返回值（通过特殊变量 "return"）
runtime.AddOp(KonOpCode.Env_SetLocalEnv, "return");

// 5. 恢复环境
runtime.AddOp(KonOpCode.Env_Rise);
```

#### 2. Host 函数 (KnHostFunction)

```csharp
// 直接调用 C# 方法
var result = hostFunc.Func(args.ToArray());
runtime.GetCurrentFiber().OperandStack.PushValue(result);
```

#### 3. Continuation (KnContinuation)

```csharp
// 恢复保存的栈和环境状态
currentFiber.OperandStack.Restore(continuation.SavedOperandStack);
currentFiber.InstructionStack.Restore(continuation.SavedInstructionStack);
currentFiber.CurrentEnvId = continuation.SavedEnvId;

// 将参数追加到恢复的操作数栈
foreach (var arg in args)
{
    currentFiber.OperandStack.PushValue(arg);
}
```

---

## 指令批次操作

### OpBatch 机制

**代码位置**: `src/Kon.Interpreter/Runtime/KonLangRuntimeExt.cs`

```csharp
// 开始批次
runtime.OpBatchStart();

// 添加操作
runtime.AddOp(KonOpCode.ValStack_PushFrame);
runtime.AddOp(KonOpCode.Node_RunNode, someNode);
runtime.AddOp(KonOpCode.ValStack_PopFrameAndPushTopVal);

// 提交批次（反序压入指令栈）
runtime.OpBatchCommit();
```

**关键点**：
- 批次中的指令会**以相反顺序**压入指令栈
- 这样执行顺序与添加顺序一致
- 支持嵌套批次

**示例**：
```csharp
AddOp(A);  // 先添加
AddOp(B);
AddOp(C);  // 后添加

// 压入指令栈时: [C, B, A]
// 执行顺序: A → B → C ✓
```

---

## 执行流程示例

### 示例：执行 `(+ 1 2)`

#### 1. 解析阶段
```
源码: "(+ 1 2)"

解析为 KnChainNode:
- Core: KnWord("+")
- CallType: PrefixCall
- CallParams: [KnInt64(1), KnInt64(2)]
```

#### 2. 初始化运行时
```csharp
var runtime = new KonInterpreterRuntime();
var rootFiber = Fiber.CreateRootFiber();
runtime.FiberMgr.AddFiber(rootFiber);
```

#### 3. 展开为指令
```
ChainExprHandler.ExpandChainNodeExpr() 生成指令:

InstructionStack: [
    ValStack_PushFrame,
    Node_IterEvalChainNode(chainNode),
    ValStack_PopFrameAndPushTopVal
]
```

#### 4. 执行循环

**迭代 1**: `ValStack_PushFrame`
```
OperandStack: [] → []  (Frame 创建)
```

**迭代 2**: `Node_IterEvalChainNode`
```
检测到 PrefixCall，展开为:
- Node_RunNode("+")
- Node_RunNode(1)
- Node_RunNode(2)
- Ctrl_ApplyToFrameBottom
```

**迭代 3**: `Node_RunNode("+")`
```
NodeHandler.ExpandNode():
- 查找环境中的 "+" → 找到内置函数
- 压入操作数栈

OperandStack: [+_func]
```

**迭代 4**: `Node_RunNode(1)`
```
直接压入值

OperandStack: [+_func, 1]
```

**迭代 5**: `Node_RunNode(2)`
```
直接压入值

OperandStack: [+_func, 1, 2]
```

**迭代 6**: `Ctrl_ApplyToFrameBottom`
```
FuncHandler.RunApplyToFrameBottom():
1. 弹出栈帧: [+_func, 1, 2]
2. func = +_func, args = [1, 2]
3. 调用 HostFunction: +(1, 2) = 3
4. 压入结果

OperandStack: [3]
```

**迭代 7**: `ValStack_PopFrameAndPushTopVal`
```
弹出栈帧，保留顶值

OperandStack: [3]  (Frame 已弹出)
```

**迭代 8**: `OpStack_LandSuccess`
```
退出执行循环
```

#### 5. 返回结果
```csharp
return currentFiber.OperandStack.PeekBottomOfAllFrames();  // KnInt64(3)
```

---

## 关键设计模式总结

### 1. 指令驱动执行
- 所有代码先编译为指令序列
- 执行时按指令逐步处理
- 避免直接递归，利于调试和控制流

### 2. 栈帧隔离
- 每个表达式/函数调用有独立栈帧
- 自动管理临时值的生命周期
- 清晰的作用域边界

### 3. 可扩展注册表
- OpCode 和 Keyword 通过注册表管理
- 可动态添加新指令和关键字
- 核心引擎无需修改

### 4. 词法作用域
- 环境形成树状结构
- 闭包捕获定义时的环境
- 支持嵌套作用域

### 5. 协作式多任务
- Fiber 提供轻量级并发
- 通过状态转换实现调度
- 支持挂起和恢复

### 6. Continuation 支持
- 保存和恢复执行状态
- 支持效果处理（Effect System）
- 一等公民的 Continuation

---

## 下一步：实现 InstanceCall

### 当前缺失的部分

1. **`Node_RunGetProperty` OpCode 实现**
   - 需要从对象中获取字段/方法
   - 需要处理 `KnObject` 类型

2. **`this` 上下文绑定**
   - 方法调用时需要绑定 `this`
   - 可能需要特殊的环境变量或参数传递

3. **方法查找机制**
   - 从 `KnObject.GetMethod()` 获取方法体
   - 如果是字段访问，从 `KnObject.GetField()` 获取

### 实现策略

#### 方案 1：扩展 Node_RunGetProperty
```csharp
// 注册新的指令处理器
registry.RegisterInstructionHandler(
    KonOpCode.Node_RunGetProperty,
    ObjectHandler.RunGetProperty
);

// 实现处理器
public static void RunGetProperty(KonInterpreterRuntime runtime, Instruction instruction)
{
    var chainNode = (KnChainNode)instruction.Memo;
    var obj = runtime.GetCurrentFiber().OperandStack.PeekTop(); // 获取对象

    if (obj is KnObject ksObject)
    {
        var propertyName = GetPropertyName(chainNode.Core);

        // 尝试获取方法
        var method = ksObject.GetMethod(propertyName);
        if (method != null)
        {
            runtime.GetCurrentFiber().OperandStack.PushValue(method);
            return;
        }

        // 尝试获取字段
        var field = ksObject.GetField(propertyName);
        if (field != null)
        {
            runtime.GetCurrentFiber().OperandStack.PushValue(field);
            return;
        }

        throw new Exception($"Property '{propertyName}' not found");
    }
}
```

#### 方案 2：添加 `this` 绑定
- 创建特殊的 `BoundMethod` 类型
- 包含方法体和 `this` 对象引用
- 在 `RunApplyToFunc` 中处理 `BoundMethod`

---

## 参考资料

### 代码位置索引

- **Runtime Core**: `src/Kon.Interpreter/Runtime/`
  - `KonInterpreterRuntime.cs` - 运行时状态
  - `KonInterpreterEngine.cs` - 执行引擎
  - `StackMachine.cs` - 通用栈机制
  - `OperandStack.cs`, `InstructionStack.cs` - 双栈
  - `Instruction.cs` - 指令结构
  - `KonOpCode.cs` - OpCode 定义

- **Environment**: `src/Kon.Interpreter/Runtime/`
  - `Env.cs` - 环境/作用域
  - `EnvTree.cs` - 环境树

- **Fiber**: `src/Kon.Interpreter/Runtime/`
  - `Fiber.cs` - Fiber 定义
  - `FiberManager.cs` - Fiber 调度器
  - `FiberState.cs` - Fiber 状态

- **Handlers**: `src/Kon.Interpreter/Handlers/`
  - `Call/ChainExprHandler.cs` - 链式表达式处理
  - `PrefixKeyword/FuncHandler.cs` - 函数处理
  - `ValueStackHandler.cs` - 值栈操作
  - `EnvHandler.cs` - 环境操作
  - `NodeHandler.cs` - 节点处理

- **Extension**: `src/Kon.Interpreter/`
  - `Runtime/ExtensionRegistry.cs` - 扩展注册表
  - `Capsule/ExtensionRegistryInitializer.cs` - 初始化器

---

**文档版本**: 2025-10-26
**作者**: Claude Code
**目的**: 为实现 InstanceCall 集成提供架构理解基础
