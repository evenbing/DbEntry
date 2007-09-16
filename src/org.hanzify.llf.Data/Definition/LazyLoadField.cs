
using System;
using System.Collections.Generic;
using System.Text;
using Lephone.Data.Common;
using Lephone.Data.Builder;
using Lephone.Data.SqlEntry;
using Lephone.Util;
using Lephone.Util.Logging;

namespace Lephone.Data.Definition
{
    public class LazyLoadField<T> : LazyLoadOneBase<T>
    {
        public LazyLoadField(object owner)
            : base(owner)
        {
        }

        protected override void DoLoad()
        {
            ObjectInfo oi = DbObjectHelper.GetObjectInfo(owner.GetType());
            string kn = oi.KeyFields[0].Name;
            object key = oi.KeyFields[0].GetValue(owner);
            SelectStatementBuilder sb = new SelectStatementBuilder(oi.From, null, null);
            sb.Where.Conditions = CK.K[kn] == key;
            sb.Keys.Add(RelationName);
            SqlStatement sql = sb.ToSqlStatement(context.Dialect);
            oi.LogSql(sql);
            object o = context.ExecuteScalar(sql);
            if (o == DBNull.Value)
            {
                o = null;
            }
            m_Value = (T)o;
        }
    }
}