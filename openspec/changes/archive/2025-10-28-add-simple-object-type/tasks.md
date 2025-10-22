# Implementation Tasks

## 1. 创建 KnObject 节点类型
- [x] 1.1 在 `src/Kon.Core/Node/` 创建 `KnObject.cs`
- [x] 1.2 继承 `KnNodeBase` 并实现 `KnNode` 接口
- [x] 1.3 添加字段存储（Dictionary<string, KnNode>）
- [x] 1.4 添加方法存储（Dictionary<string, KnNode>）
- [x] 1.5 实现基础构造函数和初始化逻辑

## 2. 实现对象字段管理 API
- [x] 2.1 实现 `SetField(string name, KnNode value)` 方法
- [x] 2.2 实现 `GetField(string name)` 方法，返回 KnNode 或 null
- [x] 2.3 实现 `HasField(string name)` 方法
- [x] 2.4 实现 `RemoveField(string name)` 方法（可选）
- [x] 2.5 添加字段访问的异常处理

## 3. 实现对象方法管理 API
- [x] 3.1 实现 `AddMethod(string name, KnNode methodBody)` 方法
- [x] 3.2 实现 `GetMethod(string name)` 方法
- [x] 3.3 实现 `HasMethod(string name)` 方法
- [x] 3.4 确保方法体与 Kon 函数定义格式兼容

## 4. 实现 InstanceCall 支持
- [x] 4.1 在 `src/Kon.Interpreter/Handlers/Call/` 创建或修改处理器（创建了 GetPropertyHandler.cs）
- [x] 4.2 添加 `KnChainNode.InstanceCall` 的处理逻辑（修改了 ChainExprHandler.cs）
- [x] 4.3 实现方法查找机制（从 KnObject 中获取方法）
- [x] 4.4 实现 `this` 上下文绑定（方法调用时绑定当前对象为第一个参数）
- [x] 4.5 将方法体提交给双栈解释器执行（通过 RunApplyToFunc）
- [x] 4.6 处理方法参数传递和返回值（在 FuncHandler 中实现）

## 5. 运行时集成
- [x] 5.1 确保 `KnObject` 可以存储在操作数栈中
- [x] 5.2 确保 `KnObject` 可以存储在环境变量中
- [x] 5.3 验证 `KnObject` 与 `StackMachine` 的兼容性

## 6. 编写 C# 单元测试
- [x] 6.1 在 `tests/Kon.Interpreter.Tests/` 创建 `ObjectTests.cs`
- [x] 6.2 编写对象创建测试
- [x] 6.3 编写字段读写测试（SetField/GetField）
- [x] 6.4 编写方法定义和获取测试
- [x] 6.5 编写实例方法调用测试（无参数）- **已完成，7个新测试**
- [x] 6.6 编写实例方法调用测试（带参数）- **已完成**
- [x] 6.7 编写 `this` 上下文访问测试 - **已完成**
- [x] 6.8 编写边界情况测试（访问不存在的字段/方法）

## 7. 验证和文档
- [x] 7.1 运行所有测试并确保通过（27 个 ObjectTests + 94 个其他测试 = 121 个测试全部通过）
- [x] 7.2 验证不违反"不修改 Kon 解析器"的约束
- [x] 7.3 确认与双栈模型兼容（KnObject 继承 KnNodeBase）
- [x] 7.4 添加必要的代码注释和 XML 文档
- [x] 7.5 更新相关文档（如有必要）- **已创建 Kon-interpreter-architecture.md 和 type-projection-and-dispatch-strategies.md**

## 8. 代码审查和优化
- [x] 8.1 代码审查：检查命名规范和代码风格
- [x] 8.2 性能检查：确保对象操作不影响解释器性能
- [x] 8.3 内存管理：检查是否有内存泄漏风险
- [x] 8.4 错误处理：确保所有异常情况都有适当处理

## Dependencies
- 任务 2-3 依赖任务 1（需要先有 KnObject 类）
- 任务 4 依赖任务 3（需要方法存储机制）
- 任务 6 可以与任务 1-5 并行进行（TDD 方式）
- 任务 7-8 依赖所有前序任务完成

## Parallelizable Work
- 任务 2 和任务 3 可以并行开发（字段和方法管理相互独立）
- 任务 6 的不同测试用例可以并行编写
