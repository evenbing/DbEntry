﻿using Leafing.Data.Builder;
using Leafing.Data.Builder.Clause;
using Leafing.Data.Definition;
using Leafing.Data.Dialect;
using Leafing.Data.SqlEntry;
using NUnit.Framework;

namespace Leafing.UnitTest.Data
{
    [TestFixture]
    public class AlterTableTest : SqlTestBase
    {
        [DbContext("SqlServerMock")]
        public class MyUser : DbObjectModel<MyUser>
        {
            public string Name { get; set; }
            public int Age { get; set; }
            [Length(50), AllowNull] public string Nick { get; set; }
        }

        [Test]
        public void Test1()
        {
            var ab = new AlterTableStatementBuilder(new FromClause("User"))
                         {
                             AddColumn = new ColumnInfo("Age", typeof(int), false, false, false, false, 0, 0, null),
                             DefaultValue = 0,
                         };
            var sql = ab.ToSqlStatement(new SqlServer2005(), null);
            Assert.AreEqual("ALTER TABLE [User] ADD COLUMN [Age] INT NOT NULL DEFAULT(0);", sql.SqlCommandText);
        }

        [Test]
        public void Test1A()
        {
            MyUser.AddColumn(p => p.Age).Default(0).AlterTable();
            AssertSql("ALTER TABLE [My_User] ADD COLUMN [Age] INT NOT NULL DEFAULT(0);<Text><30>()");
        }

        [Test]
        public void Test2()
        {
            var ab = new AlterTableStatementBuilder(new FromClause("User"))
            {
                AddColumn = new ColumnInfo("Nick", typeof(string), false, false, true, true, 50, 0, null),
            };
            var sql = ab.ToSqlStatement(new SqlServer2005(), null);
            Assert.AreEqual("ALTER TABLE [User] ADD COLUMN [Nick] NVARCHAR (50) NULL;", sql.SqlCommandText);
        }

        [Test]
        public void Test2A()
        {
            MyUser.AddColumn(p => p.Nick).AlterTable();
            AssertSql("ALTER TABLE [My_User] ADD COLUMN [Nick] NVARCHAR (50) NULL;<Text><30>()");
        }
    }
}