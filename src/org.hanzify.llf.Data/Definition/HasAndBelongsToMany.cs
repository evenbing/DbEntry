
#region usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Lephone.Data;
using Lephone.Data.Common;
using Lephone.Data.Driver;

#endregion

namespace Lephone.Data.Definition
{
    public class HasAndBelongsToMany<T> : LazyLoadListBase<T>, IHasAndBelongsToManyRelations
    {
        private OrderBy Order;

        private List<object> _SavedNewRelations = new List<object>();
        List<object> IHasAndBelongsToManyRelations.SavedNewRelations { get { return _SavedNewRelations; } }

        private List<object> _RemovedRelations = new List<object>();
        List<object> IHasAndBelongsToManyRelations.RemovedRelations { get { return _RemovedRelations; } }

        internal HasAndBelongsToMany(object owner)
            : base(owner)
        {
            this.Order = new OrderBy();
            InitForeignKeyName();
        }

        public HasAndBelongsToMany(object owner, OrderBy Order)
            : base(owner)
        {
            this.Order = Order;
            InitForeignKeyName();
        }

        public HasAndBelongsToMany(object owner, string OrderByString)
            : base(owner)
        {
            this.Order = OrderBy.Parse(OrderByString);
            InitForeignKeyName();
        }

        private void InitForeignKeyName()
        {
            ObjectInfo oi = DbObjectHelper.GetObjectInfo(owner.GetType());
            MemberHandler mh = oi.GetHasAndBelongsToMany(typeof(T));
            ForeignKeyName = mh.Name;
        }

        protected override void InnerWrite(object item, bool IsLoad)
        {
            if (m_IsLoaded)
            {
                ObjectInfo oi = DbObjectHelper.GetObjectInfo(item.GetType());
                if (oi.HasOnePremarykey)
                {
                    object key = oi.Handler.GetKeyValue(item);
                    if (!key.Equals(oi.KeyFields[0].UnsavedValue))
                    {
                        _SavedNewRelations.Add(key);
                    }
                }
                else
                {
                    throw new DbEntryException("HasAndBelongsToMany relation need the class has one primary key.");
                }
            }
        }

        protected override IList<T> InnerLoad()
        {
            ObjectInfo oi = DbObjectHelper.GetObjectInfo(owner.GetType());
            object key = oi.KeyFields[0].GetValue(owner);
            DbObjectList<T> il = new DbObjectList<T>();
            context.FillCollection(il, typeof(T), oi.ManyToManyMediFrom,
                CK.K[ForeignKeyName] == key, Order, null);
            return il;
        }

        protected override void OnRemoveItem(T item)
        {
            ObjectInfo oi = DbObjectHelper.GetObjectInfo(item.GetType());
            object key = oi.Handler.GetKeyValue(item);
            if (key == oi.KeyFields[0].UnsavedValue)
            {
                _SavedNewRelations.Remove(key);
            }
            else
            {
                _RemovedRelations.Add(key);
            }
        }
    }
}