﻿using System;
using Lephone.Data.Common;
using Lephone.Data.Definition;

namespace Lephone.Data.QuerySyntax
{
    [Serializable]
    public class QueryContent<T> : IWhere<T>, IAfterWhere<T>, IRangeable<T>, IGetPagedSelector where T : class, IDbObject
    {
        protected internal WhereCondition m_where;
        protected internal OrderBy m_order;
        protected internal Range m_range;
        protected internal DbContext m_entry;
        protected internal int m_pagesize;

        public QueryContent(DbContext entry)
        {
            m_entry = entry;
        }

        protected internal QueryContent(QueryContent<T> content)
        {
            if (content != null)
            {
                m_where = content.m_where;
                m_order = content.m_order;
                m_range = content.m_range;
                m_entry = content.m_entry;
            }
        }

        public IAfterWhere<T> Where(WhereCondition where)
        {
            m_where = where;
            return this;
        }

        public IRangeable<T> OrderBy(string key)
        {
            return OrderBy(new OrderBy(key));
        }

        public IRangeable<T> OrderBy(params ASC[] os)
        {
            m_order = new OrderBy(os);
            return this;
        }

        public IRangeable<T> OrderBy(OrderBy order)
        {
            m_order = order;
            return this;
        }

        public ISelectable<T> Range(int startIndex, int endIndex)
        {
            m_range = new Range(startIndex, endIndex);
            return this;
        }

        public ISelectable<T> Range(Range r)
        {
            if (r != null)
            {
                m_range = r;
            }
            return this;
        }

        public DbObjectList<T> Select()
        {
            var ret = new DbObjectList<T>();
            m_entry.FillCollection(ret, typeof(T), m_where, m_order, m_range);
            return ret;
        }

        public DbObjectList<T> SelectDistinct()
        {
            var ret = new DbObjectList<T>();
            m_entry.FillCollection(ret, typeof(T), m_where, m_order, m_range, true);
            return ret;
        }

        public IPagedSelector GetPagedSelector()
        {
            return new PagedSelector<T>(m_where, m_order, m_pagesize, m_entry);
        }

        public IPagedSelector GetDistinctPagedSelector()
        {
            return new PagedSelector<T>(m_where, m_order, m_pagesize, m_entry, true);
        }

        public IPagedSelector GetStaticPagedSelector()
        {
            return new StaticPagedSelector<T>(m_where, m_order, m_pagesize, m_entry);
        }

        public IPagedSelector GetDistinctStaticPagedSelector()
        {
            return new StaticPagedSelector<T>(m_where, m_order, m_pagesize, m_entry, true);
        }

        public long GetCount()
        {
            return m_entry.GetResultCount(typeof(T), m_where);
        }

        public decimal? GetMax(string columnName)
        {
            return m_entry.GetMax(typeof(T), m_where, columnName);
        }

        public decimal? GetMin(string columnName)
        {
            return m_entry.GetMin(typeof(T), m_where, columnName);
        }

        public DateTime? GetMaxDate(string columnName)
        {
            return m_entry.GetMaxDate(typeof(T), m_where, columnName);
        }

        public DateTime? GetMinDate(string columnName)
        {
            return m_entry.GetMinDate(typeof(T), m_where, columnName);
        }

        public decimal? GetSum(string columnName)
        {
            return m_entry.GetSum(typeof(T), m_where, columnName);
        }

        public DbObjectList<GroupByObject<T1>> GroupBy<T1>(string columnName)
        {
            return m_entry.GetGroupBy<T1>(typeof(T), m_where, m_order, columnName);
        }

        public DbObjectList<GroupBySumObject<T1, T2>> GroupBySum<T1, T2>(string groupbyColumnName, string sumColumnName)
        {
            return m_entry.GetGroupBySum<T1, T2>(typeof(T), m_where, m_order, groupbyColumnName, sumColumnName);
        }

        public IGetPagedSelector PageSize(int pageSize)
        {
            m_pagesize = pageSize;
            return this;
        }
    }
}
