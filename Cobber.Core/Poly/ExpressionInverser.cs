﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobber.Core.Poly.Expressions;

namespace Cobber.Core.Poly
{
    static class ExpressionInverser
    {
        public static Expression InverseExpression(Expression exp)
        {
            Expression ret = new VariableExpression();
            Expression s = exp;
            do
            {
                ret = s.GenerateInverse(ret);
                foreach (var i in s.Children)
                    if (i.HasVariable)
                    {
                        s = i;
                        break;
                    }
            }
            while (!(s is VariableExpression));
            return ret;
        }
    }
}
