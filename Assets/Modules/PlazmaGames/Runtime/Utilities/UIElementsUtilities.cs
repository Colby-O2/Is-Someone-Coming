using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlazmaGames.Core.Utils
{
    public static class ContentFitterUtilities
    {
        public static void RefreshContentFitter(RectTransform transform)
        {
            if (transform == null || !transform.gameObject.activeSelf)
            {
                return;
            }

            foreach (RectTransform child in transform)
            {
                RefreshContentFitter(child);
            }

            LayoutGroup layoutGroup = transform.GetComponent<LayoutGroup>();
            ContentSizeFitter contentSizeFitter = transform.GetComponent<ContentSizeFitter>();
            if (layoutGroup != null)
            {
                layoutGroup.SetLayoutHorizontal();
                layoutGroup.SetLayoutVertical();
            }

            if (contentSizeFitter != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform);
            }
        }
    }

    public static class RectTransformExtension
    {
        private static int CountCornersVisibleFrom(this RectTransform rectTransform, Camera camera = null)
        {
            Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height);
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);

            int visibleCorners = 0;
            Vector3 tempScreenSpaceCorner;
            for (var i = 0; i < objectCorners.Length; i++)
            {
                if (camera != null)
                    tempScreenSpaceCorner = camera.WorldToScreenPoint(objectCorners[i]);
                else
                {
                    tempScreenSpaceCorner = objectCorners[i];
                }

                if (screenBounds.Contains(tempScreenSpaceCorner))
                {
                    visibleCorners++;
                }
            }
            return visibleCorners;
        }

        public static bool IsFullyVisibleFrom(this RectTransform rectTransform, Camera camera = null)
        {
            if (!rectTransform.gameObject.activeInHierarchy)
                return false;

            return CountCornersVisibleFrom(rectTransform, camera) == 4;
        }

        public static bool IsVisibleFrom(this RectTransform rectTransform, Camera camera = null)
        {
            if (!rectTransform.gameObject.activeInHierarchy)
                return false;

            return CountCornersVisibleFrom(rectTransform, camera) > 0;
        }
    }

    public static class DropdownUtilities
    {
        public static void SetDropdownOptions<T>(ref TMP_Dropdown dropdown, List<string> additional = null, List<T> ignore = null) where T : System.Enum
        {
            dropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

            if (additional != null)
            {
                foreach (string val in additional)
                {
                    TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
                    option.text = val;
                    options.Add(option);
                }
            }

            foreach (T val in System.Enum.GetValues(typeof(T)))
            {
                if (ignore != null && ignore.Contains(val)) continue;

                TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
                option.text = val.ToString();
                options.Add(option);
            }

            dropdown.AddOptions(options);
        }

        public static void SetDropdownOptions(ref TMP_Dropdown dropdown, List<string> rawOptions)
        {
            dropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

            if (options != null)
            {
                foreach (string val in rawOptions)
                {
                    TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
                    option.text = val;
                    options.Add(option);
                }
            }

            dropdown.AddOptions(options);
        }

        public static void SetDropdownOptions<T>(ref Dropdown dropdown, List<string> additional = null, List<T> ignore = null) where T : System.Enum
        {
            dropdown.ClearOptions();

            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

            if (additional != null)
            {
                foreach (string val in additional)
                {
                    Dropdown.OptionData option = new Dropdown.OptionData();
                    option.text = val;
                    options.Add(option);
                }
            }

            foreach (T val in System.Enum.GetValues(typeof(T)))
            {
                if (ignore != null && ignore.Contains(val)) continue;

                Dropdown.OptionData option = new Dropdown.OptionData();
                option.text = val.ToString();
                options.Add(option);
            }

            dropdown.AddOptions(options);
        }
    }

    public static class GridLayoutGroupUtilities
    {
        /// <summary>
        /// Get the size of a flexible GridLayoutGroup
        /// </summary>
        private static Vector2Int GetFlexibleSize(this GridLayoutGroup grid)
        {
            int itemsCount = grid.transform.childCount;
            float prevX = float.NegativeInfinity;
            int xCount = 0;

            for (int i = 0; i < itemsCount; i++)
            {
                Vector2 pos = ((RectTransform)grid.transform.GetChild(i)).anchoredPosition;

                if (pos.x <= prevX)
                    break;

                prevX = pos.x;
                xCount++;
            }

            int yCount = GetAnotherAxisCount(itemsCount, xCount);
            return new Vector2Int(xCount, yCount);
        }

        /// <summary>
        /// Gets the number of element on a axis.
        /// </summary>
        private static int GetAnotherAxisCount(int totalCount, int axisCount)
        {
            return totalCount / axisCount + Mathf.Min(1, totalCount % axisCount);
        }

        /// <summary>
        /// Get The size of a GridLayoutGroup grouop
        /// </summary>
        public static Vector2Int GetSize(this GridLayoutGroup grid)
        {
            int itemsCount = grid.transform.childCount;
            Vector2Int size = Vector2Int.zero;

            if (itemsCount == 0)
                return size;

            switch (grid.constraint)
            {
                case GridLayoutGroup.Constraint.FixedColumnCount:
                    size.x = grid.constraintCount;
                    size.y = GetAnotherAxisCount(itemsCount, size.x);
                    break;

                case GridLayoutGroup.Constraint.FixedRowCount:
                    size.y = grid.constraintCount;
                    size.x = GetAnotherAxisCount(itemsCount, size.y);
                    break;

                case GridLayoutGroup.Constraint.Flexible:
                    size = GetFlexibleSize(grid);
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Unexpected constraint: {grid.constraint}");
            }

            return size;
        }
    }
}
