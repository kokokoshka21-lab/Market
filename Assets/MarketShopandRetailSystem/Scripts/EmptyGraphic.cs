using UnityEngine.UI;

namespace MarketShopandRetailSystem
{
	public class EmptyGraphic : Graphic
	{
		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
		}
	}
}
