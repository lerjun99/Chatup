using System;
namespace ChatUp.Domain.Entities
{
    /// <summary>Leaves the property (or the whole entity when put on a class) out of the audit log change details.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true)]
    public sealed class AuditIgnoreAttribute : Attribute { }

    /// <summary>Logs that the property changed, but never its value (secrets, personal identifiers).</summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class AuditMaskAttribute : Attribute { }
}
