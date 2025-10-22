# 项目上下文

## 项目目的

ExtensibleScopedRowType 是一个实验性编程语言项目，旨在实现基于**可扩展作用域行类型**（Extensible Scoped Row Type）的类型系统，并结合效果系统（Effect System）。

### 核心目标

1. **设计并实现 Kon 数据格式**：一种类似 Lisp/XML/JSON 混合体的数据格式，作为类似 Lisp S-expression 的语法载体，便于灵活调整语义设计而无需修改语法解析器
2. **实现可扩展作用域行类型系统**：
   - Row Type（行类型）：结构化类型，可以合并和扩展
   - Scoped Row Type：追踪每个 row 的来源，支持作用域限定访问
   - Extensible Row：支持行参数化和泛型
3. **实现完整的面向对象特性**：
   - 多继承（实继承 + 虚继承）
   - C3 线性化算法的方法解析顺序（MRO）
   - Type Class / Trait 机制
   - 访问控制（private/protected/internal/public）
4. **集成效果系统**：
   - 同步/异步双语义的行成员
   - Effect 行的组合与传递（colorless function）
   - 受检异常机制（checked exceptions）

### 设计理念（来源：知乎@酱紫君）

行类型不仅仅是 interface，它支持：
- **来源追踪**：`(T3 as T1)::b` 和 `(T3 as T2)::b` 可以访问不同来源的同名成员
- **静态鸭子类型**：结构化子类型判断（A <: B 当且仅当 A 包含 B 的所有 row）
- **Method Resolve Qualifier**：inherit/virtual/override/final 精确控制方法转发
- **Effect as Row**：将异步、IO、错误等 effect 视为行成员进行组合和传递

## 技术栈

- **.NET 8.0** - 运行时平台
- **C# 12.0** - 主要编程语言（启用 nullable 和 implicit usings）
- **Visual Studio 2022 / Rider** - 推荐 IDE
- **xUnit** - 单元测试框架（推测）
- **Git** - 版本控制

## 项目约定

### 代码风格

- **语言版本**：C# 12.0，TargetFramework: net8.0
- **Nullable**：启用可空引用类型检查
- **命名规范**：
  - 类名、方法名、属性名：PascalCase
  - 私有字段：`_camelCase`（下划线前缀）
  - 局部变量、参数：camelCase
- **文件组织**：每个类型一个文件，文件名与类型名一致

### 架构模式

#### 1. 总体架构决策（重要！）

**未来的演进路径**：

```
Kon.Core（数据格式解析）
    ↓
Kon.Interpreter（双栈解释器）
    ↓
【第一阶段】类型编译（TypeSystem 静态检查）
    ↓
【第二阶段】将编译后的 TypeSystem 对象挂载到 KonInterpreterRuntime
    ↓
【第三阶段】解释执行（带类型信息的运行时）
```

**关键架构决策点**：

1. **解释器模型**：基于 Kon.Interpreter 的**面向栈的双栈解释器**
   - 操作数栈（Operand Stack）
   - 指令栈（Instruction Stack）
   - 参考传统栈式虚拟机的设计

2. **类型编译与执行分离**：
   - **先进行类型编译**（静态类型检查阶段）
   - **类型编译通过后**，将 `TypeSystem` 对象作为 `KonInterpreterRuntime` 的属性挂载
   - **然后再执行**解释器（运行时阶段）

3. **与旧项目的关系**：
   - `DeprecateRowTypeSystem.Script` 项目的**语法已废弃**（语法有巨大变化）
   - **仅移植功能，不移植语法**
   - 原因：改用 Kon 数据格式作为语法载体（类似 Lisp S-expression）
   - 新解释器只参考旧项目的**解释执行思路**，不复用语法解析部分

4. **Kon 作为实验性语法载体**：
   - Kon 提供类似 Lisp S-expression 的灵活性
   - 目标：便于语言特性实验，无需修改解析器即可调整语义
   - 通过数据结构描述语法树，而非定义固定语法

#### 2. 分层架构

```
┌─────────────────────────────────────┐
│   Kon.Core（数据格式层）            │
│   - Node 抽象                        │
│   - Converter                        │
│   - 不可修改解析器！                  │
└─────────────────────────────────────┘
                ↓
┌─────────────────────────────────────┐
│   Kon.Interpreter（解释器层）       │
│   - KonInterpreterRuntime           │
│   - 双栈模型（操作数栈 + 指令栈）      │
│   - 未来挂载 TypeSystem 对象          │
└─────────────────────────────────────┘
                ↓
┌─────────────────────────────────────┐
│   RowTypeSystem.Core（类型系统层）   │
│   - TypeSystem（编译后挂载到 Runtime）│
│   - TypeRegistry                     │
│   - MRO 计算                         │
└─────────────────────────────────────┘
```

#### 3. 类型系统架构

- **TypeRegistry**：全局类型注册表，管理所有类型符号
- **TypeSymbol 层次结构**：
  - `TypeSymbol`（基类）
  - `RowTypeSymbol`（行类型）
  - `ClassTypeSymbol`（类类型）
  - `TypeParameter`（类型参数）
- **TypeProjection**：类型投影机制，支持 `(T as Base)::member` 语法
- **C3Linearization**：计算 MRO（Method Resolution Order）
- **RowQualifier**：方法限定词（Inherit/Virtual/Override/Final）

#### 4. 运行时架构

- **ExecutionContext**：执行上下文，管理作用域和 effect 栈
- **ClassDefinition**：类定义，分离类型定义和类型符号
- **Value**：运行时值表示
- **MethodBody**：方法体，支持动态绑定
- **KonInterpreterRuntime**（未来）：将包含 `TypeSystem` 属性

#### 5. Kon 数据格式特性

- **Node 抽象**：统一的节点表示（类似 AST）
- **Converter**：在 Kon 和其他格式间转换
- **灵活性**：可调整语义而无需修改解析器
- **实验性语法**：类似 Lisp S-expression，便于快速迭代语言特性

### 测试策略

#### 测试组织

每个核心项目都有对应的测试项目：
- `RowTypeSystem.Core` → `RowTypeSystem.Core.Tests`
- `Kon.Core` → `Kon.Core.Tests`
- `Kon.Interpreter` → `Kon.Interpreter.Tests`
- `DeprecateRowTypeSystem.Script` → `DeprecateRowTypeSystem.Script.Tests`（已废弃，仅供参考）

#### 测试驱动开发流程

**严格要求**：
1. 每个阶段代码编写完毕后，必须编写并运行单元测试
2. 所有测试 case 必须通过后，中断等待人工验证
3. 确认无误后，提交代码，进入下一阶段
4. 参考 `KonParserTests.cs` 中的 `ChainUseAsScript` 等测试作为目标

#### 测试覆盖范围

- 类型系统核心功能（MRO、继承、trait）
- Kon 解析和转换
- 解释器执行语义（双栈模型）
- Edge cases（菱形继承、虚方法、访问控制）

### Git 工作流

- **主分支**：`main`
- **提交约定**：
  - `feat(row):` - 行类型相关新功能
  - `feat(Kon):` - Kon 相关功能
  - `refactor` - 重构
  - `add` - 添加新功能
  - `fix` - 修复 bug
- **提交前检查**：确保所有测试通过

## 领域知识上下文

### Row Type（行类型）

行类型是一种**结构化类型系统**，将类型表示为一组带名字的字段（row）：

```typescript
type T = {
    a: () -> int,
    b: () -> str,
}
```

#### Row 合并

使用 `&` 合并多个 row type：

```typescript
type T3 = T1 & T2 = {
    T1::a: () -> int,
    T1::b: () -> str,
    T2::b: () -> int,  // 同名但来源不同
    T2::c: () -> bool,
}
```

#### Scoped 访问

```csharp
(T3 as T1)::b()  // 调用 T1 来源的 b，返回 str
(T3 as T2)::b()  // 调用 T2 来源的 b，返回 int
T3.b()           // 按 MRO 顺序，找到第一个，返回 str
```

### Extensible Row（可扩展行）

支持行参数化：

```typescript
type T1<P, Q> = {
    a: () -> P,
    b: () -> str,
    ..Q         // 开放行参数
}

// 闭合行
type Closed = { a: int, ..never }
```

### Subtyping（子类型）

**静态鸭子类型**：结构化子类型判断
- 如果 A 包含 B 的所有 row，则 `A <: B`
- 与继承关系无关，纯结构化判断

### 继承模式

#### 实继承（Real Inheritance）
- 父类存为成员变量
- 接口不需要重新实现，直接转发
- 带访问修饰符：`private` / `protected` / `internal` / `public`

#### 虚继承（Virtual Inheritance）
- 父类不存储，接口必须重新实现
- 类似 interface 或 abstract class

示例：
```csharp
class K1(A, B) { }              // 默认 private 实继承
class K2(virtual A, public B) { } // A 虚继承，B public 实继承
```

### Method Resolution Order (MRO)

使用 **C3 线性化算法**计算方法解析顺序，满足三个原则：
1. **扩展一致性**：子类在父类之前
2. **局部优先**：声明顺序靠前的优先
3. **单调性**：子类的 MRO 与父类的 MRO 一致

菱形继承示例（来自原文）：
```csharp
class A(object) {}
class B(object) {}
class C(object) {}
class K1(C, A, B) {}
class K2(B, D, E) {}
class K3(A, D) {}
class Z(K1, K3, K2) {}

// MRO(Z) = [Z, K1, C, K3, A, K2, B, D, E, object]
```

### Method Resolve Qualifier（方法限定词）

精确控制方法的转发和重写行为：

- **inherit**：手动转发父类同名方法
- **virtual**：虚方法，子类必须重写（除非子类也是虚基类）
- **override**：重写父类方法（父类方法不能是 final）
- **final**：禁止子类重写
- **_**（默认）：默认转发

**重要规则**："一虚皆虚" - 编译期可以直接裁剪掉虚方法后面的所有 row

### Type Class / Trait

Trait 的 row 指针永远指向最上方（不参与 MRO 排序）：

```csharp
trait ToString {
    def to_string(self): str
}

class B(A) {
    def to_string(self): str { "B" }
}

// 调用行为
B::to_string(b)        // "B"
ToString::to_string(b) // "B"（指向最上方）
A::to_string(b)        // "A"（实继承转发）
```

### Effect System

将效果（异步、IO、错误）视为行成员：

```typescript
type File = {
    read: str,           // 同步版本
    async read: str,     // 异步版本
    ..IoError,          // Error effect
    ..Print,            // IO effect
}
```

特性：
- **Contextual Effect Polymorphism**：同名 row 在不同上下文选择不同实现
- **Colorless Function**：effect 可以传递而不丢失
- **Checked Exceptions**：未处理的 effect 在类型中可见

## 重要约束

### 架构约束（关键决策点！）

1. **解释器架构必须基于 Kon.Interpreter**：
   - 使用双栈模型（操作数栈 + 指令栈）
   - 参考面向栈的编程语言解释器设计

2. **执行流程必须是三阶段**：
   - 阶段 1：类型编译（静态类型检查）
   - 阶段 2：将 `TypeSystem` 对象挂载到 `KonInterpreterRuntime` 的属性
   - 阶段 3：解释执行（运行时）

3. **与 DeprecateRowTypeSystem.Script 的关系**：
   - **只移植功能，不移植语法**
   - 原项目语法已废弃（语法有巨大变化）
   - 改用 Kon 数据格式作为语法载体
   - 可以参考解释执行思路，但不复用语法解析

4. **Kon 解析器不可修改**：
   - `Kon.Core` 的解析器**严禁修改**
   - 如果有解析问题，必须中断并询问是否先修复解析器
   - 所有语义调整必须通过 Kon 数据结构表达，而非修改解析器

### 技术约束

1. **.NET 版本**：必须使用 .NET 8.0
2. **C# 版本**：C# 12.0，启用 nullable 和 implicit usings
3. **类型安全**：类型系统必须保证静态类型安全
4. **C3 算法正确性**：MRO 计算必须符合 C3 线性化的三个原则

### 开发流程约束

**严格的开发流程**（来自 `prompt/执行任务链.md`）：

1. **必须按顺序阅读**：
   - `prompt/目标要实现的类型系统.md`
   - `prompt/关键语义实现进度.md`
   - `src/Kon.Core`
   - `src/Kon.Interpreter`
   - `tests/Kon.Core.Tests`
   - `tests/Kon.Interpreter.Tests`
   - `src/RowTypeSystem.Core`

2. **开发流程**：
   - 编写代码
   - 编写单元测试
   - 运行所有测试，确保通过
   - **中断等待人工验证**
   - 确认无误后提交代码
   - 进入下一阶段

3. **测试优先**：
   - 以 `KonParserTests.cs` 中的 `ChainUseAsScript` 等测试为目标
   - 所有功能必须有对应测试 case

## 功能实现状态

### 已实现 ✅

- Row 合并（`&` 操作）
- 来源追踪（Scoped Row）
- 行类型 spread（`..` / `..never`）
- 方法限定词（inherit/virtual/override/final）
- C3 线性化算法（MRO）
- Trait 行为（Type Class）
- 运行期 effect scope
- 行类型泛型 / 开放行参数
- Kon 数据格式解析器（Kon.Core）
- 双栈解释器（Kon.Interpreter）

### 待实现 🚧（渐进式开发路径）

#### 第一阶段：在 Kon.Interpreter 上添加基础对象能力

1. **添加简单的对象类型**：
   - 在 Kon.Interpreter 基础上支持创建简单对象
   - 对象只包含字段和方法（无继承、无类型系统）
   - 使用 Kon 语法描述对象定义

2. **实现实例方法调用**：
   - 实现 `KnChainNode.InstanceCall` 的方法调用
   - 方法体使用现有的双栈解释器执行

3. **对象创建和字段访问**：
   - 支持对象实例化
   - 支持字段读写
   - 在 `KonInterpreterRuntime` 中管理对象实例

**注**：具体的调用语法和访问语法在 Kon 语言设计中与常规语法有很大区别，将在未来详细说明。

#### 第二阶段：逐步添加类型系统功能

4. **简单继承支持**：
   - 在对象基础上添加单继承
   - 实现方法查找和转发
   - 为后续 MRO 奠定基础

5. **集成 TypeSystem 到 Runtime**：
   - 在 `KonInterpreterRuntime` 中添加 `TypeSystem` 属性
   - 实现类型编译阶段（在解释执行之前）
   - 类型编译通过后挂载 TypeSystem 对象

6. **多继承和 MRO**：
   - 支持多继承语法
   - 集成 C3 线性化算法
   - 实现基于 MRO 的方法解析

7. **虚继承和实继承**：
   - 区分虚继承和实继承
   - 实现实继承的字段存储
   - 虚继承的接口约束检查

8. **Method Qualifier 支持**：
   - 支持 inherit/virtual/override/final 限定词
   - 实现"一虚皆虚"的静态裁剪
   - 方法转发的精确控制

#### 第三阶段：高级类型系统特性

9. **Trait / Type Class**：
   - 实现 trait 定义和实现
   - Trait 的 row 指针指向最上方
   - Type class 约束检查

10. **Type Projection（类型投影）**：
    - 支持通过类型投影访问特定来源的成员
    - 在 Kon 脚本中实现类型投影功能
    - Scoped 访问的完整支持

**注**：具体的类型投影语法在 Kon 语言中的实现方式与常规语法不同，将在未来详细说明。

11. **类型参数与泛型实例化**：
    - 在 row-type 定义中声明类型参数
    - 类上实例化泛型 row
    - 泛型约束检查

12. **细粒度访问控制**：
    - Runtime 真正 enforcing private/protected/internal 访问界限
    - 编译时访问权限检查
    - 跨包访问控制

#### 第四阶段：Effect System

13. **同步/异步双语义**：
    - 同名 row 在不同上下文（sync/async）选择不同实现
    - Contextual effect polymorphism

14. **Effect 行的组合**：
    - Effect 作为行成员进行聚合、挑选（pick）
    - "colorless function" 语义

15. **异常/错误行处理**：
    - Try/pick/merge effect row 操作
    - Checked exceptions 机制

## 外部依赖

- **.NET 8.0 SDK**：编译和运行环境
- **Visual Studio 2022 / JetBrains Rider**：开发 IDE
- 无其他外部服务或 API 依赖（纯本地类型系统实现）

## 项目结构说明

### 核心项目

- **Kon.Core**：数据格式实现（**不可修改解析器**）
  - `Node/`：节点定义
  - `Converter/`：格式转换
  - `Util/`：工具类

- **Kon.Interpreter**：Kon 脚本解释器
  - 双栈模型（操作数栈 + 指令栈）
  - `KonInterpreterRuntime`（未来将挂载 TypeSystem）

- **RowTypeSystem.Core**：类型系统核心
  - `Types/`：类型符号定义
  - `Runtime/`：运行时支持
  - `TypeSystem.cs`：类型系统入口（将挂载到 Runtime）

### 已废弃项目（仅供参考）

- **DeprecateRowTypeSystem.Script**：早期脚本系统
  - **语法已废弃**，不再使用
  - 可参考**解释执行思路**
  - **不复用语法解析部分**

- **DeprecateRowTypeSystem.Cli**：早期 CLI（仅供参考）

### 辅助目录

- **prompt/**：设计文档和实现进度跟踪（**非常重要！必读**）
  - `目标要实现的类型系统.md`
  - `关键语义实现进度.md`
  - `执行任务链.md`
- **samples/**：示例代码
- **openspec/**：项目规范和变更提案
