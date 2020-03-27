using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GitMerge.Utilities
{
    public class PageView
    {
        public int PageIndex { get; set; } = 0;
        public int NumElementsPerPage { get; set; } = 10;

        private Vector2 scrollPosition;

        /// <summary>
        /// Draw a scroll view only a limited number of elements displayed.
        /// Add tool to change the page to display previous/next range of elements.
        /// </summary>
        /// <param name="numMaxElements">The total number of elements to draw.</param>
        /// <param name="callbackElementDraw">Called for each element to draw. The element index to draw is passed as argument.</param>
        public void Draw(int numMaxElements, Action<int> callbackElementDraw)
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            {
                DrawPageContent(callbackElementDraw, numMaxElements);
                DrawPageNavigation(numMaxElements);
            }
            GUILayout.EndVertical();
        }

        private void DrawPageContent(Action<int> callbackElementDraw, int numMaxElements)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                int lastElementIndex = Mathf.Min((PageIndex + 1) * NumElementsPerPage, numMaxElements);
                for (int index = PageIndex * NumElementsPerPage; index < lastElementIndex; ++index)
                {
                    callbackElementDraw(index);
                }

                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

        private void DrawPageNavigation(int numMaxElements)
        {
            int numPages = CalculateNumberOfPages(numMaxElements);

            if (numPages == 0)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Count Per Page");
                int newNumElementsPerPage = EditorGUILayout.DelayedIntField(NumElementsPerPage, GUILayout.Width(100));
                if (newNumElementsPerPage != NumElementsPerPage)
                {
                    NumElementsPerPage = Mathf.Max(newNumElementsPerPage, 1);
                    numPages = CalculateNumberOfPages(numMaxElements);
                }

                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(PageIndex == 0);
                {
                    if (GUILayout.Button("<"))
                    {
                        --PageIndex;
                    }
                }
                EditorGUI.EndDisabledGroup();

                int newPageIndex = EditorGUILayout.DelayedIntField(PageIndex + 1, GUILayout.Width(30)) - 1;
                PageIndex = Mathf.Clamp(newPageIndex, 0, numPages - 1);

                GUILayout.Label("/" + numPages);

                EditorGUI.BeginDisabledGroup(PageIndex == numPages - 1);
                {
                    if (GUILayout.Button(">"))
                    {
                        ++PageIndex;
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();
        }

        private int CalculateNumberOfPages(int numMaxElements)
        {
            int numPages = numMaxElements / NumElementsPerPage;
            if (numMaxElements % NumElementsPerPage != 0)
            {
                ++numPages;
            }

            return numPages;
        }
    }
}