﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobber.Core.Poly
{
    public abstract class ExpressionVisitor
    {
        public abstract void VisitPostOrder(Expression exp);
        public abstract void VisitPreOrder(Expression exp);
    }
}
