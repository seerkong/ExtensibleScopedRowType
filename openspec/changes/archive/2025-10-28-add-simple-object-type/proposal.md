# 添加简单对象类型支持

## Why

当前 Kon.Interpreter 只支持基础数据类型（数字、字符串、数组、字典等）和函数，缺乏对象（Object）的概念。为了实现渐进式的类型系统构建，需要首先在解释器层面添加简单的对象类型支持，作为后续实现继承、MRO、类型系统的基础。

这是 `openspec/project.md` 中"待实现功能"第一阶段的核心任务：在现有双栈解释器基础上添加基础对象能力。

## What Changes

- **新增 `KnObject` 节点类型**：作为 `KnNode` 的子类，表示运行时对象实例
- **对象字段管理**：支持对象字段的定义、读取和写入
- **对象方法支持**：支持对象方法的定义和调用
- **实例方法调用**：实现 `KnChainNode.InstanceCall` 的解释执行
- **C# 测试支持**：提供直接通过 C# 代码创建和操作 `KnObject` 的测试用例

## Impact

- **新增能力**：`Kon-object-system`（新建 spec）
- **影响代码**：
  - `src/Kon.Core/Node/` - 新增 `KnObject.cs`
  - `src/Kon.Interpreter/Runtime/` - 可能需要扩展运行时支持
  - `src/Kon.Interpreter/Handlers/` - 新增或修改调用处理器
  - `tests/Kon.Interpreter.Tests/` - 新增对象测试文件

## Constraints

- **不修改 Kon 解析器**：按照项目约束，不能修改 `Kon.Core` 的解析器部分
- **简单性优先**：对象只包含字段和方法，不包含继承、访问控制等高级特性
- **双栈模型兼容**：必须与现有的双栈解释器（操作数栈 + 指令栈）架构兼容
- **测试驱动**：必须包含完整的 C# 单元测试
