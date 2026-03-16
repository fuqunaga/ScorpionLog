#if false // Pending

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;


namespace CategorizedLogging.Editor
{
    [CustomPropertyDrawer(typeof(LogFilter))]
    public class LoggerFilter_PropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var rootContainer = new VisualElement();
            
            var filterParameters = property.FindPropertyRelative(nameof(LogFilter.filterParameters));

            Assert.IsNotNull(filterParameters);
            Assert.IsTrue(filterParameters.isArray);

            
            var innerUI = CreateCategoryModeUI(filterParameters);
            rootContainer.Add(innerUI);

            return rootContainer;
        }
        
        
        private static VisualElement CreateCategoryModeUI(SerializedProperty categoryLogLevelsProperty)
        {
            var listView = new MultiColumnListView()
            {
                headerTitle = "Category Log Levels",
                showFoldoutHeader = true,
                showAddRemoveFooter = true,
                reorderable = true,
            };


            var categoryColumn = CreateCategoryLogLevelColumn(nameof(LogFilterParameter.category));
            categoryColumn.stretchable = true;
            
            var logLevelColumn = CreateCategoryLogLevelColumn(nameof(LogFilterParameter.minimumLogLevel));
            logLevelColumn.minWidth = 150;
            
            listView.columns.Add(categoryColumn);
            listView.columns.Add(logLevelColumn);

            listView.BindProperty(categoryLogLevelsProperty);
            
            return listView;
            
            Column CreateCategoryLogLevelColumn(string bindingPath)
            {
                return new Column
                {
                    title = ObjectNames.NicifyVariableName(bindingPath),
                    bindingPath = bindingPath,
                };
            }
        }
    }
}
#endif