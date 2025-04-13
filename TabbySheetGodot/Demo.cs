using Godot;
using TabbySheet;

public partial class Demo : Node
{
	public override void _Ready()
	{
		TabbyDataSheet.Init();
		
  		foreach (var foodData in DataSheet.Load<FoodsTable>())
			GD.Print(foodData.Name);
	}
}
