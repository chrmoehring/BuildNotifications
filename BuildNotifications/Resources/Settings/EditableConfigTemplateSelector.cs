﻿using System.Windows;
using System.Windows.Controls;
using ReflectSettings.EditableConfigs;

namespace BuildNotifications.Resources.Settings
{
    internal class EditableConfigTemplateSelector : DataTemplateSelector
    {
        public static EditableConfigTemplateSelector Instance { get; } = new EditableConfigTemplateSelector();

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;

            var template = ResolveTemplate(item, element);
            return template ?? base.SelectTemplate(item, container);
        }

        private static DataTemplate ResolveTemplate(object item, FrameworkElement element)
        {
            switch (item)
            {
                case EditableString editableString:
                    if (editableString.HasPredefinedValues)
                        return element?.TryFindResource("EditableStringComboboxTemplate") as DataTemplate;
                    else
                        return DataTemplateByName(editableString, element);
                case IReadOnlyEditableCollection _:
                    return element?.TryFindResource("ReadOnlyEditableCollectionTemplate") as DataTemplate;
                case IEditableCollection editableCollection:
                    if (editableCollection.SubItemType.IsPrimitive || editableCollection.SubItemType == typeof(string))
                        return element?.TryFindResource("EditablePrimitiveCollectionTemplate") as DataTemplate;
                    else
                        return element?.TryFindResource("EditableCollectionTemplate") as DataTemplate;
                case IEditableComplex editableComplex:
                    if (editableComplex.HasPredefinedValues)
                        return element?.TryFindResource("EditableComplexComboboxTemplate") as DataTemplate;
                    else
                        return element?.TryFindResource("EditableComplexTemplate") as DataTemplate;
                case IEditableEnum _:
                    return element?.TryFindResource("EditableEnumTemplate") as DataTemplate;
                default:
                    return DataTemplateByName(item, element);
            }
        }

        private static DataTemplate? DataTemplateByName(object item, FrameworkElement element)
        {
            var type = item?.GetType();
            var name = type?.Name;
            if (name == null)
                return null;
            var expectedKey = $"{name}Template";

            return element.TryFindResource(expectedKey) as DataTemplate;
        }
    }
}