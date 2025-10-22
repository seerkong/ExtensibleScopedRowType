# Kon Object System Specification

## ADDED Requirements

### Requirement: Object Node Type
系统 SHALL 提供 `KnObject` 类型作为 `KnNode` 的子类，用于表示运行时对象实例。

#### Scenario: Create KnObject instance
- **GIVEN** Kon 运行时环境已初始化
- **WHEN** 通过 C# 代码创建 `KnObject` 实例
- **THEN** 实例成功创建且类型为 `KnNode`

#### Scenario: KnObject is a KnNode
- **GIVEN** 一个 `KnObject` 实例
- **WHEN** 检查其类型
- **THEN** 应满足 `KnNode` 接口的所有契约

### Requirement: Object Field Management
`KnObject` SHALL 支持字段（Field）的定义、读取和写入操作。

#### Scenario: Define fields in object
- **GIVEN** 一个新创建的 `KnObject` 实例
- **WHEN** 通过 C# API 设置字段 `name` 为 `"Alice"`
- **THEN** 字段成功存储在对象中

#### Scenario: Read field from object
- **GIVEN** 一个包含字段 `age = 25` 的 `KnObject`
- **WHEN** 通过 C# API 读取字段 `age`
- **THEN** 返回值为 `KnInt64(25)`

#### Scenario: Write field to object
- **GIVEN** 一个包含字段 `count = 0` 的 `KnObject`
- **WHEN** 通过 C# API 将字段 `count` 更新为 `10`
- **THEN** 字段值成功更新为 `KnInt64(10)`

#### Scenario: Access undefined field
- **GIVEN** 一个 `KnObject` 实例
- **WHEN** 尝试读取不存在的字段 `undefined`
- **THEN** 返回 `null` 或抛出明确的异常

### Requirement: Object Method Support
`KnObject` SHALL 支持方法（Method）的定义和存储，方法体应与 Kon 函数定义兼容。

#### Scenario: Define method in object
- **GIVEN** 一个 `KnObject` 实例
- **WHEN** 通过 C# API 定义方法 `greet`，方法体为返回字符串 "Hello"
- **THEN** 方法成功存储在对象中

#### Scenario: Retrieve method from object
- **GIVEN** 一个包含方法 `calculate` 的 `KnObject`
- **WHEN** 通过 C# API 获取方法 `calculate`
- **THEN** 返回对应的方法定义（KnNode 或函数对象）

#### Scenario: Method body is executable
- **GIVEN** 一个对象方法的函数体
- **WHEN** 该方法体被解释器执行
- **THEN** 应使用现有的双栈解释器执行逻辑

### Requirement: Instance Method Invocation
系统 SHALL 支持通过 `KnChainNode.InstanceCall` 调用对象实例方法。

#### Scenario: Call instance method without parameters
- **GIVEN** 一个 `KnObject` 包含方法 `getName` 返回 `"Bob"`
- **WHEN** 通过 `KnChainNode.InstanceCall` 调用 `getName`
- **THEN** 返回 `KnString("Bob")`

#### Scenario: Call instance method with parameters
- **GIVEN** 一个 `KnObject` 包含方法 `add(x, y)` 返回 `x + y`
- **WHEN** 通过 `KnChainNode.InstanceCall` 调用 `add(3, 5)`
- **THEN** 返回 `KnInt64(8)`

#### Scenario: Access 'this' context in method
- **GIVEN** 一个 `KnObject` 包含字段 `value = 10` 和方法 `getValue` 返回 `this.value`
- **WHEN** 调用实例方法 `getValue`
- **THEN** 方法能够访问 `this` 上下文并返回 `KnInt64(10)`

#### Scenario: Method not found
- **GIVEN** 一个 `KnObject` 实例
- **WHEN** 尝试调用不存在的方法 `nonExistent`
- **THEN** 抛出明确的异常或返回错误信息

### Requirement: C# API for Object Creation
系统 SHALL 提供 C# API 用于直接创建和操作 `KnObject`，无需通过 Kon 脚本解析。

#### Scenario: Create object via C# API
- **GIVEN** C# 测试代码
- **WHEN** 调用 `new KnObject()` 或工厂方法
- **THEN** 成功创建空对象实例

#### Scenario: Set field via C# API
- **GIVEN** 一个 `KnObject` 实例
- **WHEN** 调用 `obj.SetField("name", new KnString("Alice"))`
- **THEN** 字段成功设置

#### Scenario: Get field via C# API
- **GIVEN** 一个包含字段的 `KnObject`
- **WHEN** 调用 `obj.GetField("name")`
- **THEN** 返回对应的 `KnNode` 值

#### Scenario: Add method via C# API
- **GIVEN** 一个 `KnObject` 实例和一个方法定义（`KnNode` 或函数对象）
- **WHEN** 调用 `obj.AddMethod("greet", methodBody)`
- **THEN** 方法成功添加到对象

### Requirement: Integration with Interpreter Runtime
`KnObject` SHALL 与现有的 `KonInterpreterRuntime` 双栈模型兼容。

#### Scenario: KnObject on operand stack
- **GIVEN** 一个 `KnObject` 实例
- **WHEN** 将其压入操作数栈
- **THEN** 操作数栈正常处理该对象

#### Scenario: KnObject in environment
- **GIVEN** 一个 `KnObject` 实例
- **WHEN** 将其存储在环境变量中
- **THEN** 可以从环境中正确检索该对象

#### Scenario: Execute method using stack machine
- **GIVEN** 一个对象方法被调用
- **WHEN** 方法体通过 `StackMachine` 执行
- **THEN** 使用现有的指令栈和操作数栈执行逻辑

### Requirement: Test Coverage
系统 SHALL 提供完整的 C# 单元测试覆盖 `KnObject` 的所有功能。

#### Scenario: Unit tests for field operations
- **GIVEN** 测试套件
- **WHEN** 运行字段操作相关测试
- **THEN** 所有字段读写测试通过

#### Scenario: Unit tests for method invocation
- **GIVEN** 测试套件
- **WHEN** 运行方法调用相关测试
- **THEN** 所有方法调用测试通过

#### Scenario: Unit tests for edge cases
- **GIVEN** 测试套件
- **WHEN** 运行边界情况测试（如访问不存在的字段/方法）
- **THEN** 所有边界测试通过且行为符合预期
