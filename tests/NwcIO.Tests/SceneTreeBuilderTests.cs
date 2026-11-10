using NwcIO.Internal.Parsing;
using NwcIO.Model;
using Xunit;

namespace NwcIO.Tests;

public class SceneTreeBuilderTests
{
    [Fact]
    public void Build_HierarchyRecords_ConstructsNestedTree()
    {
        var records = new List<NwcPropertyRecord>
        {
            // Layer node
            new() { FirstInt = 1, Tag = 53, Name = "Level 1", Class = "Node" },
            new() { FirstInt = 2, Tag = 52, Name = "Layer", Class = "LcRevitLayer" },

            // Category collection
            new() { FirstInt = 3, Tag = 53, Name = "Walls", Class = "Node" },
            new() { FirstInt = 4, Tag = 52, Name = "Категория", Class = "LcRevitCollection" },

            // Family collection
            new() { FirstInt = 5, Tag = 53, Name = "Basic Wall", Class = "Node" },
            new() { FirstInt = 6, Tag = 52, Name = "Семейство", Class = "LcRevitCollection" },

            // Type collection
            new() { FirstInt = 7, Tag = 53, Name = "Concrete 200mm", Class = "Node" },
            new() { FirstInt = 8, Tag = 52, Name = "Тип", Class = "LcRevitCollection" },

            // Element composite
            new() { FirstInt = 9, Tag = 52, Name = "Walls: Basic Wall: Concrete 200mm", Class = "LcRevitComposite" },

            // Element properties
            new() { FirstInt = 10, Tag = 52, Name = "Length", Class = "LcRevitPropertyLength", ValueType = 4, Value = "5000 mm" },
        };

        var root = SceneTreeBuilder.Build("TestModel", records);

        Assert.Equal("TestModel", root.Name);
        Assert.Single(root.Children);

        var layer = root.Children[0];
        Assert.Equal("Level 1", layer.Name);
        Assert.Equal("LcRevitLayer", layer.Class);
        Assert.Single(layer.Children);

        var category = layer.Children[0];
        Assert.Equal("Walls", category.Name);
        Assert.Equal("LcRevitCollection", category.Class);
        Assert.Single(category.Children);

        var family = category.Children[0];
        Assert.Equal("Basic Wall", family.Name);
        Assert.Single(family.Children);

        var type = family.Children[0];
        Assert.Equal("Concrete 200mm", type.Name);
        Assert.Single(type.Children);

        var composite = type.Children[0];
        Assert.Equal("Walls: Basic Wall: Concrete 200mm", composite.Name);
        Assert.Single(composite.Properties);
        Assert.Equal("Length", composite.Properties[0].Name);
        Assert.Equal("5000 mm", composite.Properties[0].Value);
    }
}
