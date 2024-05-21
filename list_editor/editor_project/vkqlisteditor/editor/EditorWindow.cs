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

using System.Collections;
using NStack;

namespace vkqlisteditor.editor;

using Terminal.Gui;

public class EditorWindow : Window
{
	public const int CommandIndexChangeRevision = 3;
	public const int CommandIndexChangeFutureAPI = 4;
    public EditorWindow(RuntimeData runtimeData)
    {
        Title = "vkQuality Device List Editor";

        var currentY = SetupHeader(runtimeData, 0);
        
        _commandStrings = new List<string>();
        _commandStrings.Add("View device allow list");
        _commandStrings.Add("View GPU suggest allow list");
        _commandStrings.Add("View GPU suggest deny list");
        _commandStrings.Add("Change List revision number...");
        _commandStrings.Add("Change Future API level...");
        
        _commandFrameView = new FrameView ("Actions...") {
            X = 0,
            Y = currentY,
            Width = 35,
            Height = Dim.Fill (1),
            CanFocus = true,
            Shortcut = Key.CtrlMask | Key.A
        };
        _commandFrameView.Title = $"{_commandFrameView.Title} ({_commandFrameView.ShortcutTag})";
        _commandFrameView.ShortcutAction = () => _commandFrameView.SetFocus ();

        _commandListView = new ListView (_commandStrings) {
            X = 0,
            Y = 0,
            Width = Dim.Fill (0),
            Height = Dim.Fill (0),
            AllowsMarking = false,
            CanFocus = true
        };
        _commandListView.OpenSelectedItem += (a) =>
        {
	        if (runtimeData.MainProject == null) return;
	        if (a.Item == CommandIndexChangeRevision)
	        {
		        var revisionDialog = new EditNumberDialog("Change List Revision", runtimeData, a.Item);
		        Application.Run(revisionDialog);
	        }
	        else if (a.Item == CommandIndexChangeFutureAPI)
	        {
		        var futureDialog = new EditNumberDialog("Change Future API level", runtimeData, a.Item);
		        Application.Run(futureDialog);
	        }
        };
        _commandListView.SelectedItemChanged += CommandListView_SelectedChanged;
        _commandFrameView.Add (_commandListView);

        _dataFrameView = new FrameView ("Data") {
            X = 35,
            Y = currentY,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            CanFocus = true,
            Shortcut = Key.CtrlMask | Key.D
        };
        _dataFrameView.Title = $"{_dataFrameView.Title} ({_dataFrameView.ShortcutTag})";
        _dataFrameView.ShortcutAction = () => _dataFrameView.SetFocus ();

        _activeCommandIndex = 0;
        
        _dataListView = new ListView (_commandStrings) {
	        X = 0,
	        Y = 0,
	        Width = Dim.Fill (0),
	        Height = Dim.Fill (0),
	        AllowsMarking = false,
	        CanFocus = true
        };
        _deviceDataSource = new DeviceDataSource(runtimeData);
        _dataListView.Source = _deviceDataSource;
        _dataFrameView.Add(_dataListView);

        Add(_commandFrameView);
        Add(_dataFrameView);
    }

    public void UpdatePaths(RuntimeData runtimeData)
    {
        if (_projectPathLabel == null) return;
        if (!String.IsNullOrEmpty(runtimeData.ProjectPath))
        {
            _projectPathLabel.Text = $"Project path: {runtimeData.ProjectPath}";
        }
        else
        {
            _projectPathLabel.Text = "Project path: None";
        }
        
        /*
         TODO
        if (!String.IsNullOrEmpty(runtimeData.MergeProjectPath))
        {
            _mergePathLabel.Text = $"Project path: {runtimeData.MergeProjectPath}";
        }
        else
        {
            _mergePathLabel.Text = "Merge project path: None";
        }

        if (runtimeData.PlayDeviceList != null && runtimeData.PlayDeviceList.Count > 0)
        {
            _csvLoadedLabel.Text = "Play Console device list: Loaded";
        }
        else
        {
            _csvLoadedLabel.Text = "Play Console device list: Not Loaded";
        }
        */
    }
    
    private int SetupHeader(RuntimeData runtimeData, int currentY)
    {
        _projectPathLabel = new Label () {
            X = 0,
            Y = currentY,
            Width = Dim.Fill(),
            Height = 1,
        };
        ++currentY;
        /*
         TODO
        _mergePathLabel = new Label () {
            X = 0,
            Y = currentY,
            Width = Dim.Fill(),
            Height = 1,
        };
        ++currentY;
        
        _csvLoadedLabel = new Label () {
            X = 0,
            Y = currentY,
            Width = Dim.Fill(),
            Height = 1,
        };
        ++currentY;
        */
        
        UpdatePaths(runtimeData);
        Add(_projectPathLabel);        
        //Add(_mergePathLabel);        
        //Add(_csvLoadedLabel);        
        return currentY;
    }
    
    private void CommandListView_SelectedChanged (ListViewItemEventArgs e)
    {
        _activeCommandIndex = e.Item;
        _deviceDataSource.ChangeDataSource(_activeCommandIndex);
        SetNeedsDisplay();
    }

    internal class DeviceDataSource : IListDataSource
    {
	    public enum ActiveDataSource
	    {
		    DataSourceDeviceList = 0,
		    DataSourceGpuAllow,
		    DataSourceGpuDeny,
		    DataSourceCount
	    }

	    private ActiveDataSource _activeDataSource = ActiveDataSource.DataSourceDeviceList;
	    
		private readonly RuntimeData _runtimeData;
		
		public DeviceDataSource(RuntimeData runtimeData) => _runtimeData = runtimeData;

		public bool IsMarked (int item)
		{
			return false;
		}

		public int Count
		{
			get
			{
				return _activeDataSource switch
				{
					ActiveDataSource.DataSourceDeviceList => _runtimeData.DeviceAllowList.Count > 0
						? _runtimeData.DeviceAllowList.Count
						: 1,
					ActiveDataSource.DataSourceGpuAllow => _runtimeData.GpuPredictAllowList.Count > 0
						? _runtimeData.GpuPredictAllowList.Count
						: 1,
					ActiveDataSource.DataSourceGpuDeny => _runtimeData.GpuPredictDenyList.Count > 0
						? _runtimeData.GpuPredictDenyList.Count
						: 1,
					_ => 0
				};
			}
		}

		public int Length => 80;

		public void ChangeDataSource(int dataSource)
		{
			if (dataSource >= (int) ActiveDataSource.DataSourceCount) return;
			_activeDataSource = (ActiveDataSource) dataSource;
		}
		
		public void SetMark (int item, bool value)
		{
		}
		

		void RenderUstr (ConsoleDriver driver, ustring ustr, int col, int line, int width)
		{
			int byteLen = ustr.Length;
			int used = 0;
			for (int i = 0; i < byteLen;) {
				(var rune, var size) = Utf8.DecodeRune (ustr, i, i - byteLen);
				var count = Rune.ColumnWidth (rune);
				if (used+count >= width)
					break;
				driver.AddRune (rune);
				used += count;
				i += size;
			}
			for (; used < width; used++) {
				driver.AddRune (' ');
			}
		}

		public void Render (ListView container, ConsoleDriver driver, bool marked, int item, int col, int line, int width, int start = 0)
		{
			container.Move (col, line);
			string renderString = "No data present";

			switch (_activeDataSource)
			{
				case ActiveDataSource.DataSourceDeviceList:
					if (item < _runtimeData.DeviceAllowList.Count)
					{
						var dal = _runtimeData.DeviceAllowList[item];
						renderString = $"{dal.Brand},{dal.Device},{dal.MinApi},{dal.DriverVersion}";
					}
					else
					{
						renderString = "";
					}
					break;
				case ActiveDataSource.DataSourceGpuAllow:
					if (item < _runtimeData.GpuPredictAllowList.Count)
					{
						var gpul = _runtimeData.GpuPredictAllowList[item];
						renderString = $"{gpul.Brand},{gpul.DeviceName},{gpul.DeviceId},{gpul.VendorId},{gpul.MinApi},{gpul.DriverVersion}";
					}
					else
					{
						renderString = "";
					}
					break;
				case ActiveDataSource.DataSourceGpuDeny:
					if (item < _runtimeData.GpuPredictDenyList.Count)
					{
						var gpul = _runtimeData.GpuPredictDenyList[item];
						renderString = $"{gpul.Brand},{gpul.DeviceName},{gpul.DeviceId},{gpul.VendorId},{gpul.MinApi},{gpul.DriverVersion}";
					}
					else
					{
						renderString = "";
					}
					break;
			}
			RenderUstr(driver, renderString, col, line, width);
		}

		public IList ToList ()
		{
			return _activeDataSource switch
			{
				ActiveDataSource.DataSourceDeviceList => _runtimeData.DeviceAllowList,
				ActiveDataSource.DataSourceGpuAllow => _runtimeData.GpuPredictAllowList,
				ActiveDataSource.DataSourceGpuDeny => _runtimeData.GpuPredictDenyList,
				_ => throw new ArgumentOutOfRangeException()
			};
		}
    }
    
    private int _activeCommandIndex;

    private Label? _projectPathLabel;
    //private Label? _mergePathLabel;
    //private Label? _csvLoadedLabel;

    private DeviceDataSource _deviceDataSource;
    private FrameView _commandFrameView;
    private FrameView _dataFrameView;
    private ListView _commandListView;
    private ListView _dataListView;

    private List<string> _commandStrings;
}
