using UnityEngine;
using UnityEngine.UI;

namespace Aryx_MC260_Chimera
{
    [AddComponentMenu("Layout/Aryx MC260 Centered Grid Layout Group")] //having several mods use the same thing makes it really annoying in the unity editor lmao
    public class AryxCenteredGridLayoutGroup : LayoutGroup
    {
        [SerializeField, Min(1)]
        private int columns = 4;

        [SerializeField]
        private Vector2 cellSize = new Vector2(100f, 100f);

        [SerializeField]
        private Vector2 spacing = new Vector2(10f, 10f);

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            int usedColumns = Mathf.Min(columns, rectChildren.Count);

            float width = padding.horizontal + usedColumns * cellSize.x + Mathf.Max(0, usedColumns - 1) * spacing.x;

            SetLayoutInputForAxis(width, width, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            int rows = Mathf.CeilToInt(rectChildren.Count / (float)columns);

            float height = padding.vertical + rows * cellSize.y + Mathf.Max(0, rows - 1) * spacing.y;

            SetLayoutInputForAxis(height, height, -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            float availableWidth = rectTransform.rect.width - padding.horizontal;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                int row = i / columns;
                int column = i % columns;

                int rowStart = row * columns;
                int rowCount = Mathf.Min(columns, rectChildren.Count - rowStart);

                float rowWidth = rowCount * cellSize.x + Mathf.Max(0, rowCount - 1) * spacing.x;

                float rowStartX = padding.left + (availableWidth - rowWidth) * 0.5f;

                float x = rowStartX + column * (cellSize.x + spacing.x);

                SetChildAlongAxis(rectChildren[i], 0, x, cellSize.x);
            }
        }

        public override void SetLayoutVertical()
        {
            int rows = Mathf.CeilToInt(rectChildren.Count / (float)columns);

            float totalHeight = rows * cellSize.y + Mathf.Max(0, rows - 1) * spacing.y;

            float startY = GetStartOffset(1, totalHeight);

            for (int i = 0; i < rectChildren.Count; i++)
            {
                int row = i / columns;
                float y = startY + row * (cellSize.y + spacing.y);

                SetChildAlongAxis(rectChildren[i], 1, y, cellSize.y);
            }
        }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        columns = Mathf.Max(1, columns);
        cellSize.x = Mathf.Max(0f, cellSize.x);
        cellSize.y = Mathf.Max(0f, cellSize.y);
    }
#endif
    }
}
