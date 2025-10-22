namespace Kon.Core.Node.Inner;

public enum KnCallType
{
    PrefixCall,
    PostfixCall,
    // 实例属性访问/调用，使用【~】分隔
    // 对于property，~是用于进行set get
    // 对于method, ~是用于method call
    InstanceCall,
    // 静态属性/方法访问，使用【.:】分隔，
    // 例1：MyClass.:MyStaticField
    // 例2：Base.:to_string
    StaticSubscript,
    // 容器键访问，使用【::】分隔
    // 例1：myMap::'key1'
    // 例2：myList::0
    ContainerSubscript,

}
