﻿using Moq;
using System;
using System.Linq;
using System.Management.Automation;
using Treesor.Model;
using Xunit;
using static Treesor.PSDriveProvider.Test.TestDataGenerators;

namespace Treesor.PSDriveProvider.Test
{
    [Collection("Uses_powershell")]
    public class TreesorDriveItemCmdletProviderTest : IDisposable
    {
        private readonly PowerShell powershell;
        private readonly Mock<ITreesorModel> treesorModel;

        public TreesorDriveItemCmdletProviderTest()
        {
            this.treesorModel = new Mock<ITreesorModel>();

            TreesorDriveInfo.TreesorModelFactory = _ => treesorModel.Object;

            this.powershell = ShellWithDriveCreated();
        }

        public void Dispose()
        {
            this.powershell.Stop();
            this.powershell.Dispose();
        }

        #region New-Item > NewItem, MakePath

        [Fact]
        public void Powershell_creates_new_item_under_root()
        {
            // ARRANGE
            // mock result of creation of new item

            Reference<Guid> id_item = new Reference<Guid>(Guid.NewGuid());
            TreesorItem newItem = null;
            this.treesorModel
                .Setup(s => s.NewItem(It.IsAny<TreesorNodePath>(), It.IsAny<object>()))
                .Returns<TreesorNodePath, object>((p, v) => TreesorItem(p, setup: ti => newItem = ti));

            // ACT
            // create new item under root.

            this.powershell
                .AddStatement()
                    .AddCommand("New-Item")
                        .AddParameter("Path", @"custTree:\item");

            var result = this.powershell.Invoke();

            // ASSERT
            // provider writes new item on outut pipe

            this.treesorModel.VerifyAll();
            this.treesorModel.Verify(s => s.ItemExists(TreesorNodePath.Create("item")), Times.Never());
            this.treesorModel.Verify(s => s.NewItem(TreesorNodePath.Create("item"), null), Times.Once());

            Assert.False(this.powershell.HadErrors);
            Assert.IsType<TreesorItem>(result.Last().BaseObject);
            Assert.Equal(newItem.Id, ((TreesorItem)result.Last().BaseObject).Id);
            Assert.Equal(@"TreesorDriveProvider\Treesor::item", result.Last().Properties["PSPath"].Value);
        }

        [Fact]
        public void Powershell_creates_new_item_under_node()
        {
            // ARRANGE
            // mock result of creation

            TreesorItem newItem = null;
            this.treesorModel
                .Setup(s => s.NewItem(It.IsAny<TreesorNodePath>(), It.IsAny<object>()))
                .Returns<TreesorNodePath, object>((p, v) => TreesorItem(p, setup: ti => newItem = ti));

            // ACT
            // create a under item

            this.powershell
                .AddStatement()
                    .AddCommand("New-Item")
                        .AddParameter("Path", @"custTree:\item\a");

            var result = this.powershell.Invoke();

            // ASSERT
            // provider writes new item on outut pipe

            this.treesorModel.VerifyAll();
            this.treesorModel.Verify(s => s.ItemExists(TreesorNodePath.Create("item", "a")), Times.Never());
            this.treesorModel.Verify(s => s.NewItem(TreesorNodePath.Create("item", "a"), null), Times.Once());

            Assert.False(this.powershell.HadErrors);
            Assert.IsType<TreesorItem>(result.Last().BaseObject);
            Assert.Equal(newItem.Id, ((TreesorItem)result.Last().BaseObject).Id);
            Assert.Equal(@"TreesorDriveProvider\Treesor::item\a", result.Last().Properties["PSPath"].Value);
            Assert.Equal(@"TreesorDriveProvider\Treesor::item", result.Last().Properties["PSParentPath"].Value);
        }

        [Fact]
        public void Powershell_creates_new_item_under_root_twice_fails()
        {
            // ARRANGE
            // treesor service throws in existing item

            this.treesorModel
                .Setup(s => s.NewItem(It.IsAny<TreesorNodePath>(), It.IsAny<object>()))
                .Throws(TreesorModelException.DuplicateItem(TreesorNodePath.Create("item")));

            // ACT
            // create a new item under root.

            this.powershell
                .AddStatement()
                    .AddCommand("New-Item")
                        .AddParameter("Path", @"custTree:\item");

            var result = this.powershell.Invoke();

            // ASSERT
            // provider write new item on output pipe

            this.treesorModel.VerifyAll();
            this.treesorModel.Verify(s => s.ItemExists(TreesorNodePath.Create("item")), Times.Never());
            this.treesorModel.Verify(s => s.NewItem(TreesorNodePath.Create("item"), null), Times.Once());

            Assert.True(this.powershell.HadErrors);
        }

        [Fact]
        public void Powershell_creates_new_item_with_value_fails()
        {
            // ARRANGE
            // model rejecst non-null value

            this.treesorModel
                .Setup(s => s.NewItem(It.IsAny<TreesorNodePath>(), It.IsAny<object>()))
                .Throws(TreesorModelException.NotImplemented(TreesorNodePath.Create("item"), "item value"));

            // ACT

            this.powershell
                .AddStatement()
                    .AddCommand("New-Item")
                        .AddParameter("Path", @"custTree:\item")
                        .AddParameter("Value", "value");

            var result = this.powershell.Invoke();

            // ASSERT
            // provider write new item on outut pipe

            this.treesorModel.Verify(s => s.ItemExists(TreesorNodePath.Create("item")), Times.Never());
            this.treesorModel.Verify(s => s.NewItem(TreesorNodePath.Create("item"), "value"), Times.Once());
            this.treesorModel.VerifyAll();

            Assert.True(this.powershell.HadErrors);
        }

        #endregion New-Item > NewItem, MakePath

        #region Test-Path > ItemExists

        [Theory]
        [InlineData("")]
        [InlineData("item")]
        public void Powershell_asks_service_for_existence_of_existing_item(string path)
        {
            // ARRANGE
            // root exists

            this.treesorModel
                .Setup(s => s.ItemExists(TreesorNodePath.Create(path)))
                .Returns(true);

            // ACT
            // test for a root

            this.powershell
                .AddStatement()
                .AddCommand("Test-Path").AddParameter("Path", $"custTree:\\{path}");

            var result = this.powershell.Invoke();

            // ASSERT
            // item wasn't found and service was ask for the path

            this.treesorModel.VerifyAll();
            this.treesorModel.Verify(s => s.ItemExists(TreesorNodePath.Create(path)), Times.Once());

            Assert.False(this.powershell.HadErrors);
            Assert.True((bool)result.Last().BaseObject);
        }

        [Theory]
        [InlineData("")]
        [InlineData("item")]
        public void Powershell_asks_service_for_existence_of_missing_item(string path)
        {
            // ACT
            // test for a missing item

            this.powershell
                .AddStatement()
                .AddCommand("Test-Path").AddParameter("Path", $"custTree:\\{path}");

            var result = this.powershell.Invoke();

            // ASSERT
            // item wasn't found and service was asked for the path

            this.treesorModel.VerifyAll();
            this.treesorModel.Verify(s => s.ItemExists(TreesorNodePath.Create(path)), Times.Once());

            Assert.False(this.powershell.HadErrors);
            Assert.False((bool)result.Last().BaseObject);
        }

        #endregion Test-Path > ItemExists

        #region Get-Item > GetItem, MakePath

        [Theory]
        [InlineData("")]
        [InlineData("item")]
        public void Powershell_retrieves_missing_item(string path)
        {
            // ACT
            // getting a missing item fails
            this.treesorModel
                .Setup(s => s.GetItem(TreesorNodePath.Create(path)))
                .Throws(TreesorModelException.MissingItem(path));

            this.powershell
                .AddStatement()
                    .AddCommand("Get-Item").AddParameter("Path", $"custTree:\\{path}");

            var result = this.powershell.Invoke();

            // ASSERT
            // reading an item that doesn't exist is an error

            this.treesorModel.VerifyAll();
            this.treesorModel.Verify(s => s.ItemExists(TreesorNodePath.Create(path)), Times.Never());
            this.treesorModel.Verify(s => s.GetItem(TreesorNodePath.Create(path)), Times.Once());

            Assert.True(this.powershell.HadErrors);
        }

        [Theory]
        [InlineData(@"TreesorDriveProvider\Treesor::\", "")]
        [InlineData("", "item")]
        [InlineData(@"TreesorDriveProvider\Treesor::item", @"item\a")]
        public void Powershell_retrieves_existing_item(string parentPath, string path)
        {
            // ARRANGE
            // item exists

            this.treesorModel
                .Setup(s => s.GetItem(TreesorNodePath.Parse(path)))
                .Returns(TreesorItem(TreesorNodePath.Parse(path)));

            // ACT
            // retrieve item

            this.powershell
                .AddStatement()
                    .AddCommand("Get-Item")
                        .AddParameter("Path", $@"custTree:\{path}");

            var result = this.powershell.Invoke();

            // ASSERT
            // item was read and written to the pipe

            this.treesorModel.VerifyAll();
            this.treesorModel.Verify(s => s.GetItem(TreesorNodePath.Parse(path)), Times.Once());

            Assert.False(this.powershell.HadErrors);
            Assert.IsType<TreesorItem>(result.Last().BaseObject);
            Assert.Equal($@"TreesorDriveProvider\Treesor::{path}", result.Last().Properties["PSPath"].Value);
            Assert.Equal(parentPath, result.Last().Properties["PSParentPath"].Value);
        }

        #endregion Get-Item > GetItem, MakePath

        #region Set-Item > ItemExists, SetItem

        [Fact]
        public void Powershell_set_item_fails()
        {
            // ARRANGE
            // setting a value isn't supported currently

            this.treesorModel
                .Setup(s => s.SetItem(It.IsAny<TreesorNodePath>(), It.IsAny<object>()))
                .Throws(TreesorModelException.NotImplemented(TreesorNodePath.RootPath, "setting value of item(path='') isn't supported"));

            // ACT
            // setting a missing item fails

            this.powershell
                .AddStatement()
                    .AddCommand("Set-Item")
                    .AddParameter("Path", @"custTree:\")
                    .AddParameter("Value", "value");

            var result = this.powershell.Invoke();

            // ASSERT
            // ps invokes SetItem in any case to create or update the Item

            this.treesorModel.VerifyAll();
            this.treesorModel.Verify(s => s.ItemExists(TreesorNodePath.RootPath), Times.Never());
            this.treesorModel.Verify(s => s.SetItem(TreesorNodePath.RootPath, "value"), Times.Once());

            Assert.True(this.powershell.HadErrors);
        }

        #endregion Set-Item > ItemExists, SetItem

        #region Clear-Item > ItemExists, ClearItem

        [Fact]
        public void Powershell_clears_item_fails_silently()
        {
            // ACT
            // getting a missing item fails

            this.powershell
                .AddStatement()
                .AddCommand("Clear-Item").AddParameter("Path", @"custTree:\");

            var result = this.powershell.Invoke();

            // ASSERT
            // clearing an item is done always without error

            Assert.False(this.powershell.HadErrors);

            this.treesorModel.Verify(s => s.ItemExists(TreesorNodePath.RootPath), Times.Never());
            this.treesorModel.Verify(s => s.ClearItem(TreesorNodePath.RootPath), Times.Once());
            this.treesorModel.VerifyAll();
        }

        #endregion Clear-Item > ItemExists, ClearItem
    }
}