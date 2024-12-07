using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Projectables
{
    /// <summary>
    /// Declares this property or method to be Projectable. 
    /// A companion Expression tree will be generated
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ProjectableAttribute : Attribute
    {
        public ProjectableAttribute()
        {

        }
        /// <summary>
        /// Get or set how null-conditional operators are handeled
        /// </summary>
        public NullConditionalRewriteSupport NullConditionalRewriteSupport { get; set; }

        /// <summary>
        /// Get or set from which member to get the expression,
        /// or null to get it from the current member.
        /// </summary>
        public string? UseMemberBody { get; set; }

        /// <summary>
        /// Parameters values for UseMemberBody.
        /// </summary>
        public object[]? UseMemberBodyArguments { get; set; }
    }
}
