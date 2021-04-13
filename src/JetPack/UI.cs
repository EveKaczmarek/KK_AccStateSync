using UnityEngine;

namespace JetPack
{
	public partial class UI
	{
		public static void ShiftX(GameObject _obj, float _amount) => Resize(_obj, _amount, ResizeMode.ShiftX);
		public static void ShiftY(GameObject _obj, float _amount) => Resize(_obj, _amount, ResizeMode.ShiftY);

		public static void Shift(GameObject _obj, float _amountX, float _amountY)
		{
			RectTransform _rt = _obj.GetComponent<RectTransform>();
			_rt.offsetMin = new Vector2(_rt.offsetMin.x + _amountX, _rt.offsetMin.y + _amountY);
			_rt.offsetMax = new Vector2(_rt.offsetMax.x + _amountX, _rt.offsetMax.y + _amountY);
		}

		public static void Resize(GameObject _obj, float _amount, ResizeMode _mode)
		{
			RectTransform _rt = _obj.GetComponent<RectTransform>();
			if ((_mode == ResizeMode.MinX) || (_mode == ResizeMode.ShiftX))
				_rt.offsetMin = new Vector2(_rt.offsetMin.x + _amount, _rt.offsetMin.y);
			if ((_mode == ResizeMode.MaxX) || (_mode == ResizeMode.ShiftX))
				_rt.offsetMax = new Vector2(_rt.offsetMax.x + _amount, _rt.offsetMax.y);
			if ((_mode == ResizeMode.MinY) || (_mode == ResizeMode.ShiftY))
				_rt.offsetMin = new Vector2(_rt.offsetMin.x, _rt.offsetMin.y + _amount);
			if ((_mode == ResizeMode.MaxY) || (_mode == ResizeMode.ShiftY))
				_rt.offsetMax = new Vector2(_rt.offsetMax.x, _rt.offsetMax.y + _amount);
		}

		public enum ResizeMode { MinX, MaxX, MinY, MaxY, ShiftX, ShiftY }
	}
}
