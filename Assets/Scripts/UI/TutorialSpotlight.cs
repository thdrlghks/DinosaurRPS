using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // A black screen overlay with transparent rectangular windows over live UI.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialSpotlight : MaskableGraphic
    {
        private readonly List<Rect> _holes = new();
        private readonly List<float> _xs = new();
        private readonly List<float> _ys = new();

        public void SetWindows(params Rect[] holes)
        {
            _holes.Clear();
            _holes.AddRange(holes);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect bounds = rectTransform.rect;
            _xs.Clear(); _ys.Clear();
            _xs.Add(bounds.xMin); _xs.Add(bounds.xMax);
            _ys.Add(bounds.yMin); _ys.Add(bounds.yMax);
            foreach (Rect hole in _holes)
            {
                _xs.Add(Mathf.Clamp(hole.xMin, bounds.xMin, bounds.xMax));
                _xs.Add(Mathf.Clamp(hole.xMax, bounds.xMin, bounds.xMax));
                _ys.Add(Mathf.Clamp(hole.yMin, bounds.yMin, bounds.yMax));
                _ys.Add(Mathf.Clamp(hole.yMax, bounds.yMin, bounds.yMax));
            }
            _xs.Sort(); _ys.Sort();
            for (int x = 0; x < _xs.Count - 1; x++)
            for (int y = 0; y < _ys.Count - 1; y++)
            {
                Rect cell = Rect.MinMaxRect(_xs[x], _ys[y], _xs[x + 1], _ys[y + 1]);
                if (cell.width <= 0 || cell.height <= 0 || _holes.Exists(h => h.Contains(cell.center))) continue;
                int start = vh.currentVertCount;
                vh.AddVert(new Vector3(cell.xMin, cell.yMin), color, Vector2.zero);
                vh.AddVert(new Vector3(cell.xMin, cell.yMax), color, Vector2.zero);
                vh.AddVert(new Vector3(cell.xMax, cell.yMax), color, Vector2.zero);
                vh.AddVert(new Vector3(cell.xMax, cell.yMin), color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
