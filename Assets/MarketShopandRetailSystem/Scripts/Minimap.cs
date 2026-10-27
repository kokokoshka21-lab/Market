using UnityEngine;

namespace MarketShopandRetailSystem
{
	public class Minimap : MonoBehaviour
	{

		public Transform player;

		void LateUpdate()
		{
			Vector3 newPosition = player.position;
			newPosition.y = transform.position.y;
			transform.position = newPosition;

			transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
		}

		public void Click_ZoomIn()
		{
			if (GetComponent<Camera>().orthographicSize > 30)
			{
				GetComponent<Camera>().orthographicSize = GetComponent<Camera>().orthographicSize - 5;
			}
		}

		public void Click_ZoomOut()
		{
			if (GetComponent<Camera>().orthographicSize < 70)
			{
				GetComponent<Camera>().orthographicSize = GetComponent<Camera>().orthographicSize + 5;
			}
		}
	}
}