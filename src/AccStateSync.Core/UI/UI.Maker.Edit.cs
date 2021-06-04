using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;
using ParadoxNotion.Serialization;

using KKAPI.Utilities;
using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal partial class AccStateSyncUI
		{
			private Vector2 _groupScrollPos = Vector2.zero;
			private readonly GUILayoutOption _buttonSmall = GUILayout.Width(40);

			private int _curEditGroupKind = -1;

			private void DrawEditBlock()
			{
				Event _windowEvent = Event.current;

				if (!_pluginCtrl._cachedCoordinateGroupList.Any(x => x.Kind == _curEditGroupKind))
					_curEditGroupKind = -1;

				_groupScrollPos = GUILayout.BeginScrollView(_groupScrollPos);
				{
					foreach (TriggerGroup _group in _pluginCtrl._cachedCoordinateGroupList.ToList())
					{
						GUILayout.BeginHorizontal(GUI.skin.box);
						{
							if (_curRenameGroupKind == _group.Kind)
							{
								_curRenameGroupLabel = GUILayout.TextField(_curRenameGroupLabel, GUILayout.Width(65), GUILayout.ExpandWidth(false));
								GUILayout.FlexibleSpace();
								if (GUILayout.Button(new GUIContent("save", "Save group label"), _buttonSmall))
								{
									if (_curRenameGroupLabel.Trim().IsNullOrEmpty() || _curRenameGroupLabel != _group.Label)
										_pluginCtrl.RenameTriggerGroup(_curRenameGroupKind, _curRenameGroupLabel);
									RefreshCache();
								}
								if (GUILayout.Button(new GUIContent("back", "Cancel renaming"), _buttonSmall))
								{
									_curRenameGroupKind = -1;
									_curRenameGroupLabel = "";
								}
							}
							else
							{
								if (GUILayout.Button(new GUIContent(_group.Label, "Rename group label"), (_curEditGroupKind == _group.Kind ? _labelActive : _label), GUILayout.Width(70), GUILayout.ExpandWidth(false)))
								{
									if (_curRenameGroupState > -1)
										return;
									_curRenameGroupKind = _group.Kind;
									_curRenameGroupLabel = _group.Label;
								}
								GUILayout.FlexibleSpace();
								if (_curEditGroupKind != _group.Kind)
								{
									if (GUILayout.Button(new GUIContent("▼", "View the properties of this group"), _priorityElem))
										_curEditGroupKind = _group.Kind;
								}
								else
								{
									if (GUILayout.Button(new GUIContent("▲", "Collapse the property view of this group"), _priorityElem))
										_curEditGroupKind = -1;
								}
								if (GUILayout.Button(new GUIContent("X", "Remove the group and accessory settings belong to this group"), _priorityElem))
								{
									_pluginCtrl.RemoveTriggerGroup(_currentCoordinateIndex, _group.Kind);
									if (_curEditGroupKind == _group.Kind)
										_curEditGroupKind = -1;
									RefreshCache();
								}
							}
						}
						GUILayout.EndHorizontal();

						if (_curEditGroupKind == _group.Kind)
						{
							List<int> _states = _group.States.OrderBy(x => x.Key).Select(x => x.Key).ToList();

							GUILayout.BeginHorizontal(GUI.skin.box);
							{
								GUILayout.BeginVertical();
								{
									foreach (int _state in _states)
									{
										GUILayout.BeginHorizontal();
										{
											if (_curRenameGroupState == _state)
											{
												_curRenameStateLabel = GUILayout.TextField(_curRenameStateLabel, GUILayout.Width(65), GUILayout.ExpandWidth(false));
												GUILayout.FlexibleSpace();
												if (GUILayout.Button(new GUIContent("save", "Save group state label"), _buttonSmall))
												{
													if (_curRenameStateLabel.Trim().IsNullOrEmpty() || _curRenameStateLabel != _group.States[_state])
														_pluginCtrl.RenameTriggerGroupState(_curEditGroupKind, _curRenameGroupState, _curRenameStateLabel);
													RefreshCache();
												}
												if (GUILayout.Button(new GUIContent("back", "Cancel renaming"), _buttonSmall))
												{
													_curRenameGroupState = -1;
													_curRenameStateLabel = "";
												}
											}
											else
											{
												if (GUILayout.Button(new GUIContent(_group.States[_state], "Rename group label"), _label, GUILayout.Width(70), GUILayout.ExpandWidth(false)))
												{
													if (_curRenameGroupKind > -1)
														return;
													_curRenameGroupState = _state;
													_curRenameStateLabel = _group.States[_state];
												}

												GUILayout.FlexibleSpace();

												if (_state == _group.Startup)
												{
													GUILayout.Button(new GUIContent("■", "This state is set as startup"), _buttonActive, _priorityElem);
												}
												else
												{
													if (GUILayout.Button(new GUIContent(" ", "Set this state as startup"), _buttonActive, _priorityElem))
													{
														_group.Startup = _state;
														RefreshCache();
													}
												}

												if (_state == _group.Secondary)
												{
													if (GUILayout.Button(new GUIContent("■", "Remove secondary setting for this group"), _buttonActive, _priorityElem))
													{
														_group.Secondary = -1;
														RefreshCache();
													}
												}
												else
												{
													if (GUILayout.Button(new GUIContent(" ", "Set this state as secondary (assigned when H start)"), _buttonActive, _priorityElem))
													{
														_group.Secondary = _state;
														RefreshCache();
													}
												}

												if (GUILayout.Button(new GUIContent("=", "Clone the settings into a new state set"), _priorityElem))
												{
													_pluginCtrl.CloneAsNewTriggerGroupState(_currentCoordinateIndex, _group.Kind, _state);
													RefreshCache();
												}

												if (GUILayout.Button(new GUIContent("X", "Remove the state and accessory settings belong to this state"), _priorityElem))
												{
													_pluginCtrl.RemoveTriggerGroupState(_currentCoordinateIndex, _group.Kind, _state);
													RefreshCache();
													if (_state == _group.State)
														_pluginCtrl.ToggleByRefKind(_group.Kind);
												}
											}
										}
										GUILayout.EndHorizontal();
									}
								}
								GUILayout.EndVertical();
							}
							GUILayout.EndHorizontal();
						}
					}
				}
				GUILayout.EndScrollView();

				GUILayout.BeginHorizontal(GUI.skin.box);
				{
					GUILayout.Label(new GUIContent("Integrity Check", "Run a full check on current coordinate"), _label);
					GUILayout.FlexibleSpace();
					if (GUILayout.Button(new GUIContent("GO", "Check for missing or redundant triggers"), _buttonElem))
					{
						_pluginCtrl.MissingKindCheck(_currentCoordinateIndex);
						_pluginCtrl.MissingPartCheck(_currentCoordinateIndex);
						_pluginCtrl.MissingGroupCheck(_currentCoordinateIndex);
						_pluginCtrl.MissingPropertyCheck(_currentCoordinateIndex);
						RefreshCache();
						_logger.LogMessage("Integrity check finished");
					}
				}
				GUILayout.EndHorizontal();
#if DEBUG
				GUILayout.BeginHorizontal(GUI.skin.box);
				{
					GUILayout.Label("Debug", _label);
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Trigger", GUILayout.Width(65)))
					{
						List<TriggerProperty> _data = _pluginCtrl.TriggerPropertyList;
						string _json = JSONSerializer.Serialize(_data.GetType(), _data, true);
						_logger.LogInfo("[TriggerPropertyList]\n" + _json);
					}
					if (GUILayout.Button("Group", GUILayout.Width(65)))
					{
						List<TriggerGroup> _data = _pluginCtrl.TriggerGroupList;
						string _json = JSONSerializer.Serialize(_data.GetType(), _data, true);
						_logger.LogInfo("[TriggerGroupList]\n" + _json);
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal(GUI.skin.box);
				{
					GUILayout.Label("Export", _label);
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Trigger", GUILayout.Width(65)))
					{
						List<TriggerProperty> _data = _pluginCtrl.TriggerPropertyList.Where(x => x.Coordinate == _currentCoordinateIndex).ToList().JsonClone() as List<TriggerProperty>;
						_data.ForEach(x => x.Coordinate = -1);
						string _json = JSONSerializer.Serialize(_data.GetType(), _data, true);
						string _filePath = Path.Combine(_cfgExportPath.Value, $"ASS_Trigger_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.json");
						if (!Directory.Exists(_cfgExportPath.Value))
							Directory.CreateDirectory(_cfgExportPath.Value);
						File.WriteAllText(_filePath, _json);
						_logger.LogMessage($"Trigger settings exported to {_filePath}");
					}
					if (GUILayout.Button("Group", GUILayout.Width(65)))
					{
						List<TriggerGroup> _data = _pluginCtrl.TriggerGroupList.Where(x => x.Coordinate == _currentCoordinateIndex).ToList().JsonClone() as List<TriggerGroup>;
						_data.ForEach(x => x.Coordinate = -1);
						string _json = JSONSerializer.Serialize(_data.GetType(), _data, true);
						string _filePath = Path.Combine(_cfgExportPath.Value, $"ASS_Group_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.json");
						if (!Directory.Exists(_cfgExportPath.Value))
							Directory.CreateDirectory(_cfgExportPath.Value);
						File.WriteAllText(_filePath, _json);
						_logger.LogMessage($"Group settings exported to {_filePath}");
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal(GUI.skin.box);
				{
					GUILayout.Label("Import", _label);
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Trigger", GUILayout.Width(65)))
					{
						const string _fileExt = ".json";
						const string _fileFilter = "Exported Setting (*.json)|*.json|All files|*.*";
						OpenFileDialog.Show(_string => OnFileAccept(_string, "Trigger"), "Open Exported Setting", CharaMaker._savePath, _fileFilter, _fileExt);
					}
					if (GUILayout.Button("Group", GUILayout.Width(65)))
					{
						const string _fileExt = ".json";
						const string _fileFilter = "Exported Setting (*.json)|*.json|All files|*.*";
						OpenFileDialog.Show(_string => OnFileAccept(_string, "Group"), "Open Exported Setting", CharaMaker._savePath, _fileFilter, _fileExt);
					}
				}
				GUILayout.EndHorizontal();
#endif
			}

			private void OnFileAccept(string[] _string, string _mode)
			{
				if (_string == null || _string.Length == 0 || _string[0].IsNullOrEmpty()) return;
				string _filePath = _string[0];

				if (!File.Exists(_filePath))
				{
					_logger.LogMessage($"{_filePath} file doesn't exist");
					return;
				}

				CharaMaker._savePath = Path.GetDirectoryName(_filePath);

				if (_mode == "Trigger")
				{
					List<TriggerProperty> _data = JSONSerializer.Deserialize<List<TriggerProperty>>(File.ReadAllText(_filePath));
					if (_data?.Count == 0)
					{
						_logger.LogMessage($"Nothing to import");
						return;
					}
					_pluginCtrl.TriggerPropertyList.RemoveAll(x => x.Coordinate == _currentCoordinateIndex);
					_data.ForEach(x => x.Coordinate = _currentCoordinateIndex);
					_pluginCtrl.TriggerPropertyList.AddRange(_data);
				}

				if (_mode == "Group")
				{
					List<TriggerGroup> _data = JSONSerializer.Deserialize<List<TriggerGroup>>(File.ReadAllText(_filePath));
					if (_data?.Count == 0)
					{
						_logger.LogMessage($"Nothing to import");
						return;
					}
					_pluginCtrl.TriggerGroupList.RemoveAll(x => x.Coordinate == _currentCoordinateIndex);
					_data.ForEach(x => x.Coordinate = _currentCoordinateIndex);
					_pluginCtrl.TriggerGroupList.AddRange(_data);
				}
			}

			private void RefreshCache()
			{
				_curRenameGroupKind = -1;
				_curRenameGroupLabel = "";
				_curRenameGroupState = -1;
				_curRenameStateLabel = "";

				_pluginCtrl.RefreshCache();
				_pluginCtrl.AccSlotChangedHandler();
			}
		}
	}
}
