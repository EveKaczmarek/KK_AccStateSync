using UnityEngine;

namespace JetPack
{
	public partial class UI
	{
		public static void ShiftX(GameObject obj, float amount) => Resize(obj, amount, ResizeMode.ShiftX);
		public static void ShiftY(GameObject obj, float amount) => Resize(obj, amount, ResizeMode.ShiftY);
		/*
		public static void ResizeX(GameObject obj, float amount, ResizeMode mode = ResizeMode.MinX)
		{
			RectTransform RT = obj.GetComponent<RectTransform>();
			if ((mode == ResizeMode.MinX) || (mode == ResizeMode.ShiftX))
				RT.offsetMin = new Vector2(RT.offsetMin.x + amount, RT.offsetMin.y);
			if ((mode == ResizeMode.MaxX) || (mode == ResizeMode.ShiftX))
				RT.offsetMax = new Vector2(RT.offsetMax.x + amount, RT.offsetMax.y);
		}

		public static void ResizeY(GameObject obj, float amount, ResizeMode mode = ResizeMode.MinY)
		{
			RectTransform RT = obj.GetComponent<RectTransform>();
			if ((mode == ResizeMode.MinY) || (mode == ResizeMode.ShiftY))
				RT.offsetMin = new Vector2(RT.offsetMin.x, RT.offsetMin.y + amount);
			if ((mode == ResizeMode.MaxY) || (mode == ResizeMode.ShiftY))
				RT.offsetMax = new Vector2(RT.offsetMax.x, RT.offsetMax.y + amount);
		}
		*/
		public static void Shift(GameObject obj, float amountX, float amountY)
		{
			RectTransform RT = obj.GetComponent<RectTransform>();
			RT.offsetMin = new Vector2(RT.offsetMin.x + amountX, RT.offsetMin.y + amountY);
			RT.offsetMax = new Vector2(RT.offsetMax.x + amountX, RT.offsetMax.y + amountY);
		}

		public static void Resize(GameObject obj, float amount, ResizeMode mode)
		{
			RectTransform RT = obj.GetComponent<RectTransform>();
			if ((mode == ResizeMode.MinX) || (mode == ResizeMode.ShiftX))
				RT.offsetMin = new Vector2(RT.offsetMin.x + amount, RT.offsetMin.y);
			if ((mode == ResizeMode.MaxX) || (mode == ResizeMode.ShiftX))
				RT.offsetMax = new Vector2(RT.offsetMax.x + amount, RT.offsetMax.y);
			if ((mode == ResizeMode.MinY) || (mode == ResizeMode.ShiftY))
				RT.offsetMin = new Vector2(RT.offsetMin.x, RT.offsetMin.y + amount);
			if ((mode == ResizeMode.MaxY) || (mode == ResizeMode.ShiftY))
				RT.offsetMax = new Vector2(RT.offsetMax.x, RT.offsetMax.y + amount);
		}


		public static void AlignTo(GameObject obj, GameObject target, ResizeMode mode) => AlignTo(obj, target.GetComponent<RectTransform>(), mode);
		public static void AlignTo(GameObject obj, RectTransform target, ResizeMode mode)
		{
			float value = 0;
			if (mode == ResizeMode.MinX)
				value = target.offsetMin.x;
			if (mode == ResizeMode.MaxX)
				value = target.offsetMax.x;
			if (mode == ResizeMode.MinY)
				value = target.offsetMin.y;
			if (mode == ResizeMode.MaxY)
				value = target.offsetMax.y;
			ShiftTo(obj, value, mode);
		}

		public static void ShiftTo(GameObject obj, float x, float y)
		{
			RectTransform RT = obj.GetComponent<RectTransform>();
			float amountX = RT.offsetMax.x - RT.offsetMin.x;
			float amountY = RT.offsetMax.y - RT.offsetMin.y;
			RT.offsetMin = new Vector2(x, y);
			RT.offsetMax = new Vector2(x + amountX, y + amountY);
			//Core.DebugLog($"[ShiftTo][x: {x}][y: {y}][amountX: {amountX}][amountY: {amountY}]");
		}

		public static void ShiftTo(GameObject obj, float value, ResizeMode mode)
		{
			RectTransform RT = obj.GetComponent<RectTransform>();
			if ((mode == ResizeMode.MinX) || (mode == ResizeMode.ShiftX))
				RT.offsetMin = new Vector2(value, RT.offsetMin.y);
			if ((mode == ResizeMode.MaxX) || (mode == ResizeMode.ShiftX))
				RT.offsetMax = new Vector2(value, RT.offsetMax.y);
			if ((mode == ResizeMode.MinY) || (mode == ResizeMode.ShiftY))
				RT.offsetMin = new Vector2(RT.offsetMin.x, value);
			if ((mode == ResizeMode.MaxY) || (mode == ResizeMode.ShiftY))
				RT.offsetMax = new Vector2(RT.offsetMax.x, value);
		}

		public static void ResizeTo(GameObject obj, float x, float y)
		{
			RectTransform RT = obj.GetComponent<RectTransform>();
			RT.offsetMax = new Vector2(RT.offsetMin.x + x, RT.offsetMin.y + y);
		}

		public enum ResizeMode { MinX, MaxX, MinY, MaxY, ShiftX, ShiftY }
	}
}
