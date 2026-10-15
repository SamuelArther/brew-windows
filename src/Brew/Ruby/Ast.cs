namespace Brew.Ruby;

public abstract class Node
{
    public int Line;
}

public sealed class SeqNode : Node { public List<Node> Body = new(); }
public sealed class NilNode : Node { }
public sealed class TrueNode : Node { }
public sealed class FalseNode : Node { }
public sealed class SelfNode : Node { }
public sealed class IntNode : Node { public long Value; }
public sealed class FloatNode : Node { public double Value; }
public sealed class StrNode : Node { public List<object> Parts = new(); }   // string | Node
public sealed class XStrNode : Node { public StrNode Str; }
public sealed class RegexNode : Node { public StrNode Str; public string Flags; }
public sealed class SymNode : Node { public string Name; }
public sealed class DSymNode : Node { public StrNode Str; }
public sealed class ArrayNode : Node { public List<Node> Elems = new(); }
public sealed class HashNode : Node { public List<(Node key, Node value)> Pairs = new(); public bool Braces; }   // **h = (DoubleSplatNode, null)
public sealed class RangeNode : Node { public Node From, To; public bool Exclusive; }
public sealed class LocalNode : Node { public string Name; }
public sealed class IVarNode : Node { public string Name; }
public sealed class GVarNode : Node { public string Name; }
public sealed class ConstNode : Node { public Node Scope; public string Name; public bool TopLevel; }
public sealed class AssignNode : Node { public Node Target; public Node Value; }
public sealed class MultiAssignNode : Node { public List<Node> Targets = new(); public Node Value; }
public sealed class OpAssignNode : Node { public Node Target; public string Op; public Node Value; }
public sealed class SplatNode : Node { public Node Value; }
public sealed class DoubleSplatNode : Node { public Node Value; }
public sealed class BlockPassNode : Node { public Node Value; }
public sealed class AndNode : Node { public Node Left, Right; }
public sealed class OrNode : Node { public Node Left, Right; }
public sealed class NotNode : Node { public Node Value; }
public sealed class DefinedNode : Node { public Node Value; }
public sealed class IfNode : Node { public Node Cond, Then, Else; }
public sealed class WhileNode : Node { public Node Cond, Body; public bool Until; }
public sealed class ForNode : Node { public List<string> Vars; public Node Iter, Body; }
public sealed class CaseNode : Node { public Node Subject; public List<(List<Node> conds, Node body)> Whens = new(); public Node Else; }
public sealed class ReturnNode : Node { public Node Value; }
public sealed class NextNode : Node { public Node Value; }
public sealed class BreakNode : Node { public Node Value; }
public sealed class YieldNode : Node { public List<Node> Args = new(); }
public sealed class SuperNode : Node { public List<Node> Args; public BlockNode Block; }   // Args null = zsuper
public sealed class RescueClause { public List<Node> Classes = new(); public string Var; public Node Body; }
public sealed class BeginNode : Node { public Node Body; public List<RescueClause> Rescues = new(); public Node ElseBody, Ensure; }

public sealed class Param
{
    public string Name;
    public Node Default;
    public bool Splat, DoubleSplat, Block, Keyword, Required;
    public List<Param> Destructure;   // |(a, b)|
}

public sealed class BlockNode : Node { public List<Param> Params = new(); public Node Body; }
public sealed class LambdaNode : Node { public BlockNode Block; }

public sealed class CallNode : Node
{
    public Node Recv;
    public string Name;
    public List<Node> Args = new();
    public BlockNode Block;
    public bool SafeNav;
    public bool HasParens;
}

public sealed class DefNode : Node
{
    public string Name;
    public List<Param> Params = new();
    public Node Body;
    public bool Singleton;
}

public sealed class ClassNode : Node
{
    public ConstNode Path;
    public Node Super;
    public Node Body;
    public bool IsModule;
    public bool SelfClass;   // class << self
}
