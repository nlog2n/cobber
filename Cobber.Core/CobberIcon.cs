using System;
using System.Text;
using System.Collections.Generic;

namespace Cobber.Core
{
    public enum CobberIcon
    {
        None,      // Unknown icon
        Project,   // => Project
        Assembly,  // => Assembly
        Main,      // => Assembly
        Module,    // => Module
        Resource,  // resource
        Namespace, // => Namespace
        Type,      // => Type
        Interface, // => Type
        Enum,      // => Type
        Valuetype, // => Type
        Delegate,  // => Type
        Field,     // => Member
        Constant,  // => Member
        Method,    // => Member
        Constructor,  // => Member
        Omethod, // virtual or abstract method // => Member
        Property,     // => Member
        Propget,      // => Member
        Propset,      // => Member
        Event         // => Member
    }

    // for types and members
    public enum CobberIconVisible
    {
        Public, // public, no rendering. others are non-public
        Internal,
        Private,
        Protected,
        Famasm
    }

    // for members only
    public enum CobberIconOverlay
    {
        None, // no overlay
        Static
    }
}
