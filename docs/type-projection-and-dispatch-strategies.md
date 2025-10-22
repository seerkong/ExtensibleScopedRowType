# 类型投影与方法分派策略参考

本文档总结了各种编程语言中类型投影（Type Projection）和方法分派（Method Dispatch）的实现策略，为 ExtensibleScopedRowType 项目中 RowTypeSystem.Core 的 C3 Linearization 和 `as` 功能开发提供决策参考。

## 目录

- [背景与动机](#背景与动机)
- [核心概念](#核心概念)
- [语言实现案例](#语言实现案例)
  - [Rust - Trait Objects](#rust---trait-objects)
  - [C++ - Multiple Inheritance](#c---multiple-inheritance)
  - [Python - MRO](#python---mro)
  - [Scala - Trait Linearization](#scala---trait-linearization)
  - [Common Lisp CLOS](#common-lisp-clos)
- [方案对比分析](#方案对比分析)
- [ExtensibleScopedRowType 的设计决策](#extensiblescopedrowtype-的设计决策)
- [未来优化方向](#未来优化方向)

---

## 背景与动机

### 问题陈述

在 ExtensibleScopedRowType 类型系统中，我们需要支持**显式类型投影**（Explicit Type Projection）：

```typescript
// 概念示例（实际 Kon 语法会有所不同）
type T1 = { b: () -> str }
type T2 = { b: () -> int }
type T3 = T1 & T2

let obj: T3 = ...

(obj as T1)::b()  // 调用 T1 来源的 b，返回 str
(obj as T2)::b()  // 调用 T2 来源的 b，返回 int
obj.b()           // 按 MRO 顺序，返回第一个（T1::b）
```

### 核心挑战

1. **同名方法冲突**：多继承导致同名方法在不同基类中有不同实现
2. **明确调用语义**：需要支持显式指定调用哪个父类的方法
3. **性能与灵活性权衡**：静态 vtable vs 动态查找
4. **类型安全**：编译时 vs 运行时类型检查

---

## 核心概念

### 1. 类型投影（Type Projection）

将对象"视为"某个特定类型，限定方法查找的作用域。

**关键要素**：
- **源对象**：实际的对象实例
- **目标类型**：投影到的类型（通常是父类或 trait）
- **方法解析**：根据目标类型查找方法

### 2. 方法分派（Method Dispatch）

确定在运行时调用哪个具体方法的机制。

**常见策略**：
- **静态分派**：编译时确定（如 C++ 非虚函数）
- **单分派**：基于接收者类型（如 Java/C# 的虚函数）
- **多分派**：基于多个参数类型（如 Common Lisp CLOS）
- **动态分派**：运行时查找（如 Python/Ruby）

### 3. 虚函数表（vtable）

**结构**：
```
对象布局:
[vptr | data fields...]
  |
  └─> vtable: [method1_ptr, method2_ptr, ...]
```

**优点**：O(1) 方法查找，高性能
**缺点**：静态结构，难以扩展

### 4. 方法解析顺序（MRO）

在多继承场景下，确定方法查找顺序的算法（如 C3 线性化）。

---

## 语言实现案例

### 1. Rust - Trait Objects

#### 概述

Rust 通过 **trait objects** 实现动态分派，使用**胖指针**（fat pointer）存储数据指针和 vtable 指针。

#### 代码示例

```rust
trait ToString {
    fn to_string(&self) -> String;
}

trait Debug {
    fn debug(&self) -> String;
}

struct MyStruct {
    value: i32,
}

impl ToString for MyStruct {
    fn to_string(&self) -> String {
        format!("MyStruct({})", self.value)
    }
}

impl Debug for MyStruct {
    fn debug(&self) -> String {
        format!("Debug: {}", self.value)
    }
}

fn main() {
    let obj = MyStruct { value: 42 };

    // 类型投影到不同 trait
    let as_tostring: &dyn ToString = &obj;
    let as_debug: &dyn Debug = &obj;

    println!("{}", as_tostring.to_string()); // "MyStruct(42)"
    println!("{}", as_debug.debug());        // "Debug: 42"
}
```

#### 实现机制

**胖指针结构**：
```rust
struct TraitObject {
    data: *mut (),      // 指向实际对象
    vtable: *const (),  // 指向 vtable
}
```

**vtable 生成**（编译时）：
```rust
// 为 MyStruct 实现 ToString 生成的 vtable
static TOSTRING_VTABLE_FOR_MYSTRUCT: [*const (); 3] = [
    drop_in_place::<MyStruct> as *const (),
    size_of::<MyStruct> as *const (),
    to_string::<MyStruct> as *const (),
];
```

**类型转换**（`as` 操作）：
```rust
let obj: MyStruct = ...;
let trait_obj: &dyn ToString = &obj;
// 编译器生成:
// TraitObject {
//     data: &obj as *mut (),
//     vtable: &TOSTRING_VTABLE_FOR_MYSTRUCT
// }
```

#### 优缺点分析

**优点**：
- ✅ **零运行时开销**：vtable 在编译时生成
- ✅ **类型安全**：编译时检查 trait 约束
- ✅ **显式投影**：`as` 操作符明确语义
- ✅ **内存高效**：只需两个指针

**缺点**：
- ❌ **静态 vtable**：无法在运行时添加方法
- ❌ **单 trait 限制**：一个 trait object 只能表示一个 trait
- ❌ **不支持多 trait 投影**：不能同时实现多个 trait 的动态分派
- ❌ **对象布局固定**：无法像 C++ 那样调整对象内部布局

**适用场景**：
- 编译时已知所有 trait 和实现
- 性能敏感的应用
- 不需要运行时扩展

---

### 2. C++ - Multiple Inheritance

#### 概述

C++ 通过**多个 vptr** 和**指针调整**支持多继承中的类型转换。

#### 代码示例

```cpp
class Base1 {
public:
    virtual void f1() { std::cout << "Base1::f1" << std::endl; }
    virtual void common() { std::cout << "Base1::common" << std::endl; }
};

class Base2 {
public:
    virtual void f2() { std::cout << "Base2::f2" << std::endl; }
    virtual void common() { std::cout << "Base2::common" << std::endl; }
};

class Derived : public Base1, public Base2 {
public:
    void common() override { std::cout << "Derived::common" << std::endl; }
};

int main() {
    Derived* d = new Derived();

    // 类型投影
    Base1* b1 = d;  // 指针不调整，使用 Base1 的 vptr
    Base2* b2 = d;  // 指针调整，使用 Base2 的 vptr

    d->common();    // "Derived::common"
    b1->common();   // "Derived::common" (通过 vptr 调用)
    b2->common();   // "Derived::common" (通过 vptr 调用)

    // 显式调用特定基类方法
    d->Base1::common(); // "Base1::common"
    d->Base2::common(); // "Base2::common"
}
```

#### 实现机制

**对象布局**（简化）：
```
Derived 对象内存布局:
+------------------+
| Base1 subobject  |
|   vptr1 -------> Base1 vtable for Derived
|   Base1 data     |
+------------------+
| Base2 subobject  |
|   vptr2 -------> Base2 vtable for Derived
|   Base2 data     |
+------------------+
| Derived data     |
+------------------+
```

**指针调整**：
```cpp
Derived* d = new Derived();
Base2* b2 = d;  // 编译器生成: b2 = (Base2*)((char*)d + offset_of_Base2)
```

**vtable 结构**：
```
Base1 vtable for Derived:
[offset_to_top = 0]
[RTTI pointer]
[f1 -> Derived::f1 or Base1::f1]
[common -> Derived::common]

Base2 vtable for Derived:
[offset_to_top = -sizeof(Base1)]  // 负偏移，指向对象起始
[RTTI pointer]
[f2 -> Derived::f2 or Base2::f2]
[common -> Derived::common with thunk]  // thunk 调整 this 指针
```

**Thunk（桩函数）**：
```cpp
// 当通过 Base2* 调用 Derived::common 时：
void Derived_common_thunk_for_Base2(Base2* this) {
    Derived* d = (Derived*)((char*)this - offset_of_Base2);
    d->Derived::common();
}
```

#### 优缺点分析

**优点**：
- ✅ **真正的多继承**：支持复杂的继承层次
- ✅ **静态解析**：编译时确定布局和 vtable
- ✅ **显式作用域调用**：`Base1::method()` 语法
- ✅ **高性能**：单次间接跳转（vtable lookup）

**缺点**：
- ❌ **复杂的对象布局**：多个 vptr，内存开销大
- ❌ **指针调整开销**：类型转换需要计算偏移
- ❌ **菱形继承问题**：需要虚继承解决，更复杂
- ❌ **二进制兼容性差**：布局变化导致 ABI 不兼容
- ❌ **编译时绑定**：无法动态添加方法

**适用场景**：
- 需要真正的多继承
- 性能关键应用
- 复杂的类层次结构

---

### 3. Python - MRO (Method Resolution Order)

#### 概述

Python 使用 **C3 线性化算法**动态计算 MRO，支持运行时方法查找和显式父类调用。

#### 代码示例

```python
class Base1:
    def method(self):
        return "Base1"

    def common(self):
        return "Base1::common"

class Base2:
    def method(self):
        return "Base2"

    def common(self):
        return "Base2::common"

class Derived(Base1, Base2):
    def common(self):
        return "Derived::common"

# MRO 计算
print(Derived.__mro__)
# (<class 'Derived'>, <class 'Base1'>, <class 'Base2'>, <class 'object'>)

obj = Derived()

# 默认调用（按 MRO 顺序）
print(obj.method())     # "Base1" (因为 Base1 在 MRO 中更靠前)
print(obj.common())     # "Derived::common"

# 显式调用特定父类方法（类似 as 投影）
print(Base1.method(obj))  # "Base1" - 相当于 (obj as Base1)::method
print(Base2.method(obj))  # "Base2" - 相当于 (obj as Base2)::method
print(Base1.common(obj))  # "Base1::common"
print(Base2.common(obj))  # "Base2::common"

# 使用 super() (基于 MRO)
class Derived2(Base1, Base2):
    def method(self):
        return f"Derived2 -> {super().method()}"  # 调用 Base1.method

print(Derived2().method())  # "Derived2 -> Base1"
```

#### 实现机制

**MRO 存储**：
```python
# 每个类对象存储 MRO 元组
class Derived:
    __mro__ = (Derived, Base1, Base2, object)
```

**方法查找算法**（简化）：
```python
def lookup_method(obj, method_name):
    for cls in type(obj).__mro__:
        if method_name in cls.__dict__:
            return cls.__dict__[method_name]
    raise AttributeError(f"Method {method_name} not found")
```

**显式父类调用**：
```python
Base1.method(obj)
# 等价于:
# 1. 从 Base1.__dict__ 获取未绑定方法
# 2. 将 obj 作为第一个参数传入
```

**C3 线性化算法**（简化版本）：
```python
def c3_linearize(cls, *bases):
    if not bases:
        return [cls]

    # 递归计算所有基类的 MRO
    mros = [c3_linearize(base) for base in bases]
    mros.append(list(bases))

    result = [cls]
    while any(mros):
        # 选择候选：在所有 MRO 列表的头部，且不在其他 MRO 的尾部
        candidate = None
        for mro in mros:
            if not mro:
                continue
            head = mro[0]
            if not any(head in m[1:] for m in mros if m):
                candidate = head
                break

        if candidate is None:
            raise TypeError("Cannot create a consistent MRO")

        result.append(candidate)
        # 从所有 MRO 列表中移除 candidate
        for mro in mros:
            if mro and mro[0] == candidate:
                mro.pop(0)

    return result
```

#### 优缺点分析

**优点**：
- ✅ **极度灵活**：运行时可修改类和方法
- ✅ **显式父类调用**：`BaseClass.method(self)` 语法简洁
- ✅ **动态扩展**：可在运行时添加方法（monkey patching）
- ✅ **C3 算法保证**：满足本地优先、单调性等原则
- ✅ **无指针调整**：纯动态查找，无布局复杂性

**缺点**：
- ❌ **性能开销大**：每次方法调用需遍历 MRO
- ❌ **无编译时检查**：类型错误在运行时才发现
- ❌ **内存占用**：每个类需存储 MRO 和方法字典
- ❌ **难以优化**：动态性阻碍 JIT 优化

**优化技术**：
- **Method Cache**：缓存最近的方法查找结果
- **Inline Cache**：在调用点缓存方法地址（PIC - Polymorphic Inline Cache）

**适用场景**：
- 动态语言，强调灵活性
- 原型开发和快速迭代
- 不追求极致性能的应用

---

### 4. Scala - Trait Linearization

#### 概述

Scala 的 trait 系统在编译时进行**线性化**（Linearization），确定方法解析顺序，并支持 `super` 调用特定 trait。

#### 代码示例

```scala
trait A {
  def f(): String = "A"
  def common(): String = "A::common"
}

trait B {
  def f(): String = "B"
  def common(): String = "B::common"
}

class C extends A with B {
  override def common(): String = "C::common"

  // 显式调用特定 trait（仅在类内部）
  def callA(): String = super[A].common()
  def callB(): String = super[B].common()
}

object Main {
  def main(args: Array[String]): Unit = {
    val c = new C()

    println(c.f())         // "B" (因为 B 在线性化顺序中更靠前)
    println(c.common())    // "C::common"
    println(c.callA())     // "A::common"
    println(c.callB())     // "B::common"

    // 外部无法直接投影（不支持 (c as A)::common）
  }
}
```

#### 实现机制

**线性化规则**：
```scala
class C extends A with B
// 线性化: C -> B -> A -> AnyRef -> Any
```

**算法**（简化）：
```
L(C) = C ⊕ L(B) ⊕ L(A) ⊕ [A, B]
     = C ⊕ (B ⊕ A) ⊕ (A) ⊕ [A, B]
     = [C, B, A]
```

其中 `⊕` 是 merge 操作，遵循 C3 线性化规则。

**`super[Trait]` 编译**：
```scala
def callA(): String = super[A].common()

// 编译为（伪代码）：
def callA(): String = A$class.common(this)
```

编译器在编译时直接解析到 `A` 的方法。

#### 优缺点分析

**优点**：
- ✅ **编译时线性化**：性能优于 Python 的运行时 MRO
- ✅ **静态类型检查**：编译时验证 trait 约束
- ✅ **`super[Trait]` 语法**：类内部可显式调用
- ✅ **无运行时查找开销**：方法调用直接跳转

**缺点**：
- ❌ **仅限类内部**：外部无法使用 `super[Trait]` 语法
- ❌ **不支持外部投影**：无法实现 `(obj as Trait)::method`
- ❌ **静态绑定**：无法运行时修改 trait 或方法
- ❌ **学习曲线**：线性化规则对初学者不友好

**适用场景**：
- 需要静态类型安全的 trait 组合
- 性能敏感但需要多继承的场景
- 复杂的 mixin 模式

---

### 5. Common Lisp CLOS (Common Lisp Object System)

#### 概述

CLOS 提供**极其灵活的方法组合**（Method Combination）机制，支持多分派（multimethods）和运行时方法查找。

#### 代码示例

```lisp
(defclass base1 () ())
(defclass base2 () ())
(defclass derived (base1 base2) ())

;; 定义多个方法（按类特化）
(defmethod my-method ((obj base1))
  "Base1")

(defmethod my-method ((obj base2))
  "Base2")

(defmethod my-method ((obj derived))
  "Derived")

;; 默认调用（按类优先级）
(let ((obj (make-instance 'derived)))
  (my-method obj))  ; "Derived"

;; 显式调用特定类的方法
(let ((obj (make-instance 'derived)))
  (call-method
    (find-method #'my-method nil (list (find-class 'base1)))
    obj))  ; "Base1"

;; Method Combination: before/after/around
(defmethod compute ((obj derived))
  :around  ; 包裹其他方法
  (format t "Before~%")
  (call-next-method)  ; 调用下一个方法（按优先级）
  (format t "After~%"))

(defmethod compute ((obj base1))
  (format t "Base1 compute~%"))

(compute (make-instance 'derived))
; 输出:
; Before
; Base1 compute
; After
```

#### 实现机制

**多分派**：
```lisp
(defmethod foo ((a integer) (b string))
  "int-string")

(defmethod foo ((a string) (b integer))
  "string-int")

(foo 1 "hello")   ; "int-string"
(foo "hello" 1)   ; "string-int"
```

方法选择基于**所有参数的类型**，而非仅接收者。

**Generic Function**：
```lisp
;; Generic function 包含:
;; 1. 方法列表（按特化程度排序）
;; 2. Method Combination 规则
;; 3. 分派缓存

(defgeneric my-method (obj)
  (:method-combination standard))
```

**运行时方法查找**：
```lisp
;; 伪代码
(defun call-generic (gf &rest args)
  ;; 1. 计算参数的类型
  (let* ((arg-classes (mapcar #'class-of args))
         ;; 2. 查找适用的方法（按特化程度排序）
         (methods (compute-applicable-methods gf arg-classes))
         ;; 3. 应用 method combination
         (combined (combine-methods methods gf)))
    ;; 4. 调用组合后的方法
    (funcall combined args)))
```

**Method Combination 类型**：
- `standard`: 默认，`:before`、主方法、`:after`
- `+`: 所有方法结果相加
- `list`: 收集所有方法结果到列表
- 自定义组合器

#### 优缺点分析

**优点**：
- ✅ **极致灵活性**：支持任意复杂的方法组合
- ✅ **多分派**：基于所有参数类型，不仅是接收者
- ✅ **运行时扩展**：可随时添加方法
- ✅ **显式方法调用**：`call-method` 和 `find-method`
- ✅ **Method Combination**：强大的 AOP 能力

**缺点**：
- ❌ **性能极差**：运行时查找和组合，开销巨大
- ❌ **无类型检查**：完全动态，错误在运行时才发现
- ❌ **复杂度高**：学习曲线陡峭
- ❌ **难以优化**：动态性阻碍编译器优化
- ❌ **过度工程**：对大多数应用来说过于复杂

**适用场景**：
- 学术研究和探索
- 需要极端灵活性的元编程
- 不关心性能的领域（如符号计算）

---

## 方案对比分析

### 对比矩阵

| 方案 | 分派方式 | 类型检查 | 外部投影 | 性能 | 动态性 | 复杂度 |
|------|---------|---------|---------|------|--------|--------|
| **Rust Trait Objects** | 单分派（vtable） | 编译时 | ✅ 显式 `as` | ⚡⚡⚡ 高 | ❌ 静态 | 🟢 低 |
| **C++ Multiple Inheritance** | 单分派（多 vptr） | 编译时 | ⚠️ 类内 `Base::` | ⚡⚡⚡ 高 | ❌ 静态 | 🔴 高 |
| **Python MRO** | 单分派（动态查找） | 运行时 | ✅ `Base.method(obj)` | ⚡ 低 | ✅ 动态 | 🟢 低 |
| **Scala Trait Linearization** | 单分派（编译时） | 编译时 | ⚠️ 类内 `super[T]` | ⚡⚡ 中 | ❌ 静态 | 🟡 中 |
| **CLOS** | 多分派（动态） | 运行时 | ✅ `call-method` | ⚡ 极低 | ✅ 极动态 | 🔴 极高 |

### 关键维度分析

#### 1. 外部投影支持

**支持外部投影**：
- ✅ **Rust**: `let trait_obj: &dyn Trait = &obj;`
- ✅ **Python**: `BaseClass.method(obj)`
- ✅ **CLOS**: `call-method` + `find-method`

**仅类内部支持**：
- ⚠️ **C++**: `obj.Base::method()`（需在类定义时）
- ⚠️ **Scala**: `super[Trait].method()`（仅类内部）

#### 2. 性能排序

```
Rust (vtable) ≈ C++ (vptr) > Scala (编译时) > Python (MRO + cache) >> CLOS (动态查找)
```

#### 3. 灵活性排序

```
CLOS (完全动态) > Python (运行时修改) > Scala (编译时线性化) > C++ (静态多继承) > Rust (静态 trait)
```

#### 4. 实现复杂度排序

```
Rust (胖指针) ≈ Python (MRO) < Scala (线性化) < C++ (多 vptr + 指针调整) < CLOS (generic function + combination)
```

---

## ExtensibleScopedRowType 的设计决策

### 项目约束与目标

1. **Row Type 语义**：结构化类型，强调灵活性
2. **Scoped 访问**：支持 `(obj as Type)::method` 语法
3. **C3 Linearization**：已选择 MRO 算法
4. **解释器环境**：运行在 Kon.Interpreter 中
5. **迭代开发**：从简单到复杂

### 推荐方案：Python-like 动态查找 + Rust-like 绑定对象

#### 核心设计

```csharp
public class KnBoundMethod : KnNodeBase
{
    public KnObject BoundObject;      // 绑定的对象（类似 Python 的 self）
    public string MethodName;          // 方法名
    public KnNode? ProjectedType;      // as 投影的类型（可选，类似 Rust 的 vtable 切换）

    // 未来扩展
    // public IDispatchStrategy? DispatchStrategy;
    // public MethodCache? Cache;
}
```

#### 方法解析策略

```csharp
public KnNode ResolveMethod(KnBoundMethod bound)
{
    // 1. 如果有 ProjectedType，限定作用域
    if (bound.ProjectedType != null)
    {
        // 在 ProjectedType 的 MRO 中查找
        var mro = ComputeMRO(bound.ProjectedType);
        foreach (var type in mro)
        {
            if (type.HasMethod(bound.MethodName))
            {
                return type.GetMethod(bound.MethodName);
            }
        }
        throw new MethodNotFoundException();
    }

    // 2. 否则，在对象的完整 MRO 中查找
    var objectMro = ComputeMRO(bound.BoundObject.GetType());
    foreach (var type in objectMro)
    {
        if (type.HasMethod(bound.MethodName))
        {
            return type.GetMethod(bound.MethodName);
        }
    }
    throw new MethodNotFoundException();
}
```

#### 调用执行

```csharp
public void ExecuteBoundMethod(KnBoundMethod bound, List<KnNode> args)
{
    // 1. 解析方法
    var method = ResolveMethod(bound);

    // 2. 构造参数列表（self + args）
    var fullArgs = new List<KnNode> { bound.BoundObject };
    fullArgs.AddRange(args);

    // 3. 执行方法（作为纯函数）
    return ExecuteFunction(method, fullArgs);
}
```

### 设计优势

| 特性 | 实现方式 | 灵感来源 |
|------|---------|---------|
| **外部投影** | `ProjectedType` 字段 | Rust 的 `as` 操作符 |
| **方法查找** | 动态 MRO 遍历 | Python 的 MRO 算法 |
| **绑定对象** | `BoundObject` + `self` 参数 | Python 的绑定方法 + Rust trait 方法 |
| **纯函数语义** | 显式 `self` 参数 | Rust trait 方法 |
| **可扩展性** | 接口预留 | 所有语言的经验 |

### 与各方案的对比

| 维度 | 我们的方案 | 对比 |
|------|----------|------|
| **性能** | 🟡 中等（动态查找 + 缓存潜力） | 比 Rust/C++ 慢，比 CLOS 快，与 Python 相当 |
| **灵活性** | 🟢 高（运行时扩展） | 接近 Python，强于 Rust/C++/Scala |
| **外部投影** | ✅ 完全支持 | 与 Rust/Python 相同，强于 Scala/C++ |
| **类型安全** | 🟡 运行时检查 | 与 Python/CLOS 相同 |
| **实现复杂度** | 🟢 低 | 显著低于 C++/CLOS |

---

## 未来优化方向

### 阶段 1：基础实现（当前）

```csharp
public class KnBoundMethod : KnNodeBase
{
    public KnObject BoundObject;
    public string MethodName;
    public KnNode? ProjectedType;
}
```

**特点**：
- 简单直接
- 无缓存
- 每次调用都查找 MRO

---

### 阶段 2：添加 Inline Cache

```csharp
public class KnBoundMethod : KnNodeBase
{
    public KnObject BoundObject;
    public string MethodName;
    public KnNode? ProjectedType;

    // Inline Cache
    private KnNode? _cachedMethod;
    private KnNode? _cachedProjectedType;
    private int _cacheVersion;  // 对象类型版本号
}

public KnNode GetMethodWithCache()
{
    var currentVersion = BoundObject.GetTypeVersion();

    if (_cachedMethod != null &&
        _cachedProjectedType == ProjectedType &&
        _cacheVersion == currentVersion)
    {
        return _cachedMethod;
    }

    _cachedMethod = ResolveMethod(this);
    _cachedProjectedType = ProjectedType;
    _cacheVersion = currentVersion;
    return _cachedMethod;
}
```

**优点**：
- 避免重复的 MRO 查找
- 对象类型未变时，O(1) 查找

**灵感来源**：
- JavaScript 引擎（V8、SpiderMonkey）
- Python 的 method cache

---

### 阶段 3：Polymorphic Inline Cache (PIC)

```csharp
public class CallSite
{
    private struct CacheEntry
    {
        public Type ObjectType;
        public KnNode? ProjectedType;
        public KnNode Method;
    }

    private CacheEntry[] _cache = new CacheEntry[4]; // 多态缓存

    public KnNode Lookup(KnObject obj, string methodName, KnNode? projectedType)
    {
        // 1. 快速路径：检查缓存
        foreach (var entry in _cache)
        {
            if (entry.ObjectType == obj.GetType() &&
                entry.ProjectedType == projectedType)
            {
                return entry.Method;
            }
        }

        // 2. 慢速路径：MRO 查找 + 更新缓存
        var method = ResolveSlow(obj, methodName, projectedType);
        UpdateCache(obj.GetType(), projectedType, method);
        return method;
    }
}
```

**优点**：
- 支持多态调用点（同一位置调用不同类型对象）
- 业界标准优化技术

**灵感来源**：
- V8 JavaScript 引擎
- HotSpot JVM

---

### 阶段 4：可插拔的 Dispatch Strategy

```csharp
public interface IDispatchStrategy
{
    KnNode ResolveMethod(KnObject obj, string methodName, KnNode? projectedType);
}

public class MRODispatchStrategy : IDispatchStrategy
{
    public KnNode ResolveMethod(KnObject obj, string methodName, KnNode? projectedType)
    {
        var mro = ComputeMRO(projectedType ?? obj.GetType());
        // ... MRO 查找逻辑
    }
}

public class VTableDispatchStrategy : IDispatchStrategy
{
    private Dictionary<Type, VTable> _vtables = new();

    public KnNode ResolveMethod(KnObject obj, string methodName, KnNode? projectedType)
    {
        var vtable = _vtables[projectedType ?? obj.GetType()];
        return vtable.Lookup(methodName);  // O(1) 查找
    }
}

public class KnBoundMethod : KnNodeBase
{
    public KnObject BoundObject;
    public string MethodName;
    public KnNode? ProjectedType;
    public IDispatchStrategy DispatchStrategy; // 可切换策略
}
```

**优点**：
- 支持多种分派策略
- 可根据场景选择最优策略
- 便于实验和性能调优

**可选策略**：
- `MRODispatchStrategy`：灵活，支持动态修改
- `VTableDispatchStrategy`：高性能，适合稳定类型
- `CachedMRODispatchStrategy`：带缓存的 MRO

---

### 阶段 5：JIT 编译（远期）

```csharp
public class JITDispatcher
{
    public CompiledMethod Compile(KnBoundMethod bound)
    {
        // 1. 分析调用模式
        // 2. 生成优化的机器码
        // 3. 内联方法体
        // 4. 消除虚调用
    }
}
```

**技术参考**：
- PyPy（Python JIT）
- LuaJIT
- Truffle/Graal（多语言 JIT 框架）

---

## 总结与建议

### 当前阶段（MVP）

**推荐方案**：
```csharp
public class KnBoundMethod : KnNodeBase
{
    public KnObject BoundObject;
    public string MethodName;
    public KnNode? ProjectedType;
}
```

**理由**：
1. ✅ **简单直接**：易于实现和调试
2. ✅ **满足需求**：完全支持 `(obj as Type)::method` 语义
3. ✅ **灵活扩展**：为未来优化预留空间
4. ✅ **符合 Row Type 哲学**：结构化、动态、可扩展

### 中期优化（性能调优）

1. **添加 Method Cache**：缓存最近的查找结果
2. **类型版本号**：检测类型变化，使缓存失效
3. **统计调用频率**：识别热点调用路径

### 长期演进（高级特性）

1. **可插拔 Dispatch Strategy**：支持多种分派策略
2. **Polymorphic Inline Cache**：优化多态调用
3. **考虑 JIT**：如果性能成为瓶颈

### 关键原则

1. **先正确，后优化**：确保语义正确再考虑性能
2. **数据驱动优化**：基于 profiling 结果优化
3. **保持简单性**：避免过早的复杂化
4. **预留扩展性**：设计时考虑未来演进路径

---

## 参考资料

### 学术论文

1. **A Monotonic Superclass Linearization for Dylan** (Barrett et al., 1996)
   - C3 线性化算法的原始论文

2. **Traits: Composable Units of Behaviour** (Schärli et al., 2003)
   - Trait 系统的理论基础

3. **Efficient Implementation of the Smalltalk-80 System** (Deutsch & Schiffman, 1984)
   - Inline Cache 的原始论文

### 实现参考

1. **CPython Internals** - Python MRO 实现
   - `Objects/typeobject.c` - MRO 计算
   - `Objects/descrobject.c` - 方法绑定

2. **Rust Trait Object Internals**
   - `librustc_codegen_ssa/meth.rs` - vtable 生成
   - Fat pointer 布局

3. **V8 JavaScript Engine**
   - Inline Cache 实现
   - Hidden Classes (Shapes)

4. **Scala Compiler** - Trait 线性化
   - `scala.tools.nsc.transform.Mixin`

### 在线资源

- [Python MRO Explained](https://www.python.org/download/releases/2.3/mro/)
- [Rust Trait Objects](https://doc.rust-lang.org/book/ch17-02-trait-objects.html)
- [C++ Virtual Table](https://shaharmike.com/cpp/vtable-part1/)
- [Inline Caching in JavaScript](https://mathiasbynens.be/notes/shapes-ics)

---

**文档版本**: 2025-10-26
**作者**: Claude Code
**目的**: 为 RowTypeSystem.Core 的 as 功能和方法分派提供决策参考
**状态**: 活动文档，随项目演进持续更新
