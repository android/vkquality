/*
 * Copyright 2024 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace vkqlisteditor.editor;

using Terminal.Gui;

public class EditNumberDialog : Dialog 
{
    public EditNumberDialog(string dialogTitle, RuntimeData runtimeData, int commandIndex) : base(dialogTitle, 50, 10)
    {
        var textField = new TextField (GetNumber(runtimeData, commandIndex).ToString()) {
            X = 1,
            Y = 0,
            Width = 8,
            Height = 2
        };
        Add(textField);
        textField.TextChanging += TextField_TextChanging;

        void TextField_TextChanging (TextChangingEventArgs e)
        {
            int newInt = 0;
            var newString = e.NewText.ToString();
            if (!Int32.TryParse(newString, out newInt))
            {
                e.Cancel = true;
            }
        }
        
        var close = new Button ("_Ok");
        close.Clicked += () =>
        {
            var newString = textField.Text.ToString();
            int newInt = 0;
            if (Int32.TryParse(newString, out newInt))
            {
                SetNumber(runtimeData, commandIndex, newInt);
            }
            Application.RequestStop ();
        };
        AddButton (close);

        var cancel = new Button ("_Cancel");
        cancel.Clicked += ()=>Application.RequestStop();
        AddButton (cancel);
    }

    public int GetNumber(RuntimeData runtimeData, int commandIndex)
    {
        if (commandIndex == EditorWindow.CommandIndexChangeRevision)
        {
            return runtimeData.MainProject.ExportedListFileVersion;
        }
        else if (commandIndex == EditorWindow.CommandIndexChangeFutureAPI)
        {
            return runtimeData.MainProject.MinApiForFutureRecommendation;
        }

        return 0;
    }

    public void SetNumber(RuntimeData runtimeData, int commandIndex, int newInt)
    {
        if (commandIndex == EditorWindow.CommandIndexChangeRevision)
        {
            runtimeData.MainProject.ExportedListFileVersion = newInt;
        }
        else if (commandIndex == EditorWindow.CommandIndexChangeFutureAPI)
        {
            runtimeData.MainProject.MinApiForFutureRecommendation = newInt;
        }
    }

}
