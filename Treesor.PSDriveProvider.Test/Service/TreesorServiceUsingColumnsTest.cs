﻿using Elementary.Hierarchy.Collections;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;

namespace Treesor.PSDriveProvider.Test.Service
{
    [TestFixture]
    public class TreesorServiceUsingColumnsTest
    {
        private Mock<IHierarchy<string, Reference<Guid>>> hierarchyMock;
        private TreesorService treesorService;

        [SetUp]
        public void ArrangeAllTests()
        {
            this.hierarchyMock = new Mock<IHierarchy<string, Reference<Guid>>>();
            this.treesorService = new TreesorService(this.hierarchyMock.Object);
        }

        #region CreateColumn

        [Test]
        public void CreateColumn_type_string()
        {
            // ACT

            var result = this.treesorService.CreateColumn(name: "p", typename: typeof(string).Name);

            // ASSERT

            Assert.IsNotNull(result);
            Assert.AreEqual("p", result.Name);
            Assert.AreEqual(typeof(string).Name, result.TypeName);
            Assert.AreSame(result, this.treesorService.GetColumns().Single());
        }

        [Test]
        public void CreateColumn_twice_is_accepted()
        {
            // ARRANGE

            var column = this.treesorService.CreateColumn(name: "p", typename: typeof(string).Name);

            // ACT

            var result = this.treesorService.CreateColumn(name: "p", typename: typeof(string).Name);

            // ASSERT

            Assert.IsNotNull(result);
            Assert.AreSame(column, result);
            Assert.AreEqual("p", result.Name);
            Assert.AreEqual(typeof(string).Name, result.TypeName);
            Assert.AreSame(column, this.treesorService.GetColumns().Single());
        }

        [Test]
        public void CreateColumns_twice_fails_with_different_type()
        {
            // ARRANGE

            this.treesorService.CreateColumn(name: "p", typename: typeof(string).Name);

            // ACT

            var result = Assert.Throws<InvalidOperationException>(() => this.treesorService.CreateColumn(name: "p", typename: typeof(int).Name));

            // ASSERT

            Assert.AreEqual($"Column: 'p' already defined with type: '{typeof(string).Name}'", result.Message);
        }

        [Test]
        public void CreateColumns_fails_on_missing_name()
        {
            // ACT

            var result = Assert.Throws<ArgumentNullException>(() => this.treesorService.CreateColumn(null, "type"));

            // ASSERT

            Assert.AreEqual("name", result.ParamName);
        }

        [Test]
        public void CreateColumns_fails_on_missing()
        {
            // ACT

            var result = Assert.Throws<ArgumentNullException>(() => this.treesorService.CreateColumn(null, "type"));

            // ASSERT

            Assert.AreEqual("name", result.ParamName);
        }

        #endregion CreateColumn

        #region RemoveColumn

        [Test]
        public void RemoveColumn_unexisting_column_does_nothing()
        {
            // ACT

            var result = this.treesorService.RemoveColumn("p");

            // ASSERT

            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveColumn_succeeds()
        {
            // ARRANGE

            this.treesorService.CreateColumn("p", typeof(string).Name);

            // ACT

            var result = this.treesorService.RemoveColumn("p");

            // ASSERT

            Assert.IsTrue(result);
            Assert.IsFalse(this.treesorService.GetColumns().Any());
        }

        #endregion RemoveColumn

        #region RenameColumn

        [Test]
        public void RenameColumn_does_nothing_for_missing_column()
        {
            // ACT

            Assert.DoesNotThrow(() => this.treesorService.RenameColumn("p", "q"));
        }

        [Test]
        public void RenameColumn_changes_the_column_name()
        {
            // ARRANGE

            this.treesorService.CreateColumn("p", typeof(string).Name);

            // ACT

            Assert.DoesNotThrow(() => this.treesorService.RenameColumn("p", "q"));

            // ASSERT

            Assert.AreEqual("q", this.treesorService.GetColumns().Single().Name);
        }

        [Test]
        public void RenameColumn_fails_is_column_name_is_used_already()
        {
            // ARRANGE

            this.treesorService.CreateColumn("p", typeof(string).Name);
            this.treesorService.CreateColumn("q", typeof(string).Name);

            // ACT

            var result = Assert.Throws<ArgumentException>(() => this.treesorService.RenameColumn("p", "q"));

            // ASSERT

            Assert.AreEqual("An item with the same key has already been added.", result.Message);
        }

        #endregion RenameColumn
    }
}