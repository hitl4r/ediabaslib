﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BMW.Rheingold.CoreFramework.Contracts.Programming;
using BMW.Rheingold.Programming.Common;
using BMW.Rheingold.Programming.Controller.SecureCoding.Model;
using BMW.Rheingold.Psdz;
using BMW.Rheingold.Psdz.Client;
using BMW.Rheingold.Psdz.Model;
using BMW.Rheingold.Psdz.Model.Ecu;
using BMW.Rheingold.Psdz.Model.SecureCoding;
using BMW.Rheingold.Psdz.Model.SecurityManagement;
using BMW.Rheingold.Psdz.Model.Sfa;
using BMW.Rheingold.Psdz.Model.Svb;
using BMW.Rheingold.Psdz.Model.Swt;
using BMW.Rheingold.Psdz.Model.Tal;
using BMW.Rheingold.Psdz.Model.Tal.TalFilter;
using BMW.Rheingold.Psdz.Model.Tal.TalStatus;
using EdiabasLib;
using log4net;
using log4net.Config;
using PsdzClient.Core;
using PsdzClient.Programing;
using PsdzClient.Programming;

namespace PsdzClient
{
    public partial class FormMain : Form
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FormMain));

        private const string DealerId = "32395";
        private const string DefaultIp = @"127.0.0.1";
        private readonly ProgrammingJobs _programmingJobs;
        private readonly object _lockObject = new object();
        private bool _taskActive;
        private bool TaskActive
        {
            get
            {
                lock (_lockObject)
                {
                    return _taskActive;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _taskActive = value;
                }

                if (value)
                {
                    BeginInvoke((Action)(() =>
                    {
                        progressBarEvent.Style = ProgressBarStyle.Marquee;
                        labelProgressEvent.Text = string.Empty;
                    }));
                }
                else
                {
                    BeginInvoke((Action)(() =>
                    {
                        progressBarEvent.Style = ProgressBarStyle.Blocks;
                        progressBarEvent.Value = progressBarEvent.Minimum;
                        labelProgressEvent.Text = string.Empty;
                    }));
                }
            }
        }

        private bool _ignoreCheck = false;
        private bool _ignoreChange = false;
        private CancellationTokenSource _cts;
        private Dictionary<PdszDatabase.SwiRegisterEnum, List<ProgrammingJobs.OptionsItem>> _optionsDict;

        public FormMain()
        {
            InitializeComponent();

            _programmingJobs = new ProgrammingJobs(DealerId);
            _programmingJobs.UpdateStatusEvent += UpdateStatus;
            _programmingJobs.ProgressEvent += UpdateProgress;
            _programmingJobs.UpdateOptionsEvent += UpdateOptions;
        }

        private void UpdateDisplay()
        {
            bool active = TaskActive;
            bool abortPossible = _cts != null;
            bool hostRunning = false;
            bool vehicleConnected = false;
            bool talPresent = false;
            if (!active)
            {
                hostRunning = _programmingJobs.ProgrammingService != null && _programmingJobs.ProgrammingService.IsPsdzPsdzServiceHostInitialized();
            }

            if (_programmingJobs.PsdzContext?.Connection != null)
            {
                vehicleConnected = true;
                talPresent = _programmingJobs.PsdzContext?.Tal != null;
            }

            bool ipEnabled = !active && !vehicleConnected;
            bool modifyTal = !active && hostRunning && vehicleConnected && _optionsDict != null;

            textBoxIstaFolder.Enabled = !active && !hostRunning;
            comboBoxLanguage.Enabled = !active;
            ipAddressControlVehicleIp.Enabled = ipEnabled;
            checkBoxIcom.Enabled = ipEnabled;
            buttonVehicleSearch.Enabled = ipEnabled;
            buttonStartHost.Enabled = !active && !hostRunning;
            buttonStopHost.Enabled = !active && hostRunning;
            buttonConnect.Enabled = !active && hostRunning && !vehicleConnected;
            buttonDisconnect.Enabled = !active && hostRunning && vehicleConnected;
            buttonCreateOptions.Enabled = !active && hostRunning && vehicleConnected && _optionsDict == null;
            buttonModILevel.Enabled = modifyTal;
            buttonModFa.Enabled = modifyTal;
            buttonExecuteTal.Enabled = modifyTal && talPresent;
            buttonClose.Enabled = !active;
            buttonAbort.Enabled = active && abortPossible;
            checkedListBoxOptions.Enabled = !active && hostRunning && vehicleConnected;

            if (!vehicleConnected)
            {
                UpdateOptions(null);
            }
            comboBoxOptionType.Enabled = _optionsDict != null && _optionsDict.Count > 0;
        }

        private bool LoadSettings()
        {
            try
            {
                _ignoreChange = true;
                textBoxIstaFolder.Text = Properties.Settings.Default.IstaFolder;
                comboBoxLanguage.SelectedIndex = Properties.Settings.Default.LanguageIndex;
                ipAddressControlVehicleIp.Text = Properties.Settings.Default.VehicleIp;
                checkBoxIcom.Checked = Properties.Settings.Default.IcomConnection;
                if (string.IsNullOrWhiteSpace(ipAddressControlVehicleIp.Text.Trim('.')))
                {
                    ipAddressControlVehicleIp.Text = DefaultIp;
                    checkBoxIcom.Checked = false;
                }

                _programmingJobs.ClientContext.Language = comboBoxLanguage.SelectedItem.ToString();
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                _ignoreChange = false;
            }

            return true;
        }

        private bool StoreSettings()
        {
            try
            {
                Properties.Settings.Default.IstaFolder = textBoxIstaFolder.Text;
                Properties.Settings.Default.LanguageIndex = comboBoxLanguage.SelectedIndex;
                Properties.Settings.Default.VehicleIp = ipAddressControlVehicleIp.Text;
                Properties.Settings.Default.IcomConnection = checkBoxIcom.Checked;
                Properties.Settings.Default.Save();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void UpdateStatus(string message = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    UpdateStatus(message);
                }));
                return;
            }

            textBoxStatus.Text = message ?? string.Empty;
            textBoxStatus.SelectionStart = textBoxStatus.TextLength;
            textBoxStatus.Update();
            textBoxStatus.ScrollToCaret();

            UpdateDisplay();
        }

        private void UpdateProgress(int percent, bool marquee, string message = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    UpdateProgress(percent, marquee, message);
                }));
                return;
            }

            if (marquee)
            {
                progressBarEvent.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                progressBarEvent.Style = ProgressBarStyle.Blocks;
            }
            progressBarEvent.Value = percent;
            labelProgressEvent.Text = message ?? string.Empty;
        }

        private void UpdateCurrentOptions()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    UpdateCurrentOptions();
                }));
                return;
            }

            try
            {
                _ignoreChange = true;
                int selectedIndex = comboBoxOptionType.SelectedIndex;
                comboBoxOptionType.BeginUpdate();
                comboBoxOptionType.Items.Clear();
                if (_optionsDict != null)
                {
                    foreach (ProgrammingJobs.OptionType optionTypeUpdate in _programmingJobs.OptionTypes)
                    {
                        comboBoxOptionType.Items.Add(optionTypeUpdate);
                    }

                    if (selectedIndex < 0 && comboBoxOptionType.Items.Count >= 1)
                    {
                        selectedIndex = 0;
                    }

                    if (selectedIndex < comboBoxOptionType.Items.Count)
                    {
                        comboBoxOptionType.SelectedIndex = selectedIndex;
                    }
                }
            }
            finally
            {
                comboBoxOptionType.EndUpdate();
                _ignoreChange = false;
            }

            if (comboBoxOptionType.Items.Count > 0)
            {
                if (comboBoxOptionType.SelectedItem is ProgrammingJobs.OptionType optionType)
                {
                    SelectOptions(optionType.SwiRegisterEnum);
                }
                else
                {
                    SelectOptions(null);
                }
            }
        }

        private void UpdateOptions(Dictionary<PdszDatabase.SwiRegisterEnum, List<ProgrammingJobs.OptionsItem>> optionsDict)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    UpdateOptions(optionsDict);
                }));
                return;
            }

            _optionsDict = optionsDict;
            _programmingJobs.SelectedOptions = new List<ProgrammingJobs.OptionsItem>();
            UpdateCurrentOptions();
        }

        private void SelectOptions(PdszDatabase.SwiRegisterEnum? swiRegisterEnum)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    SelectOptions(swiRegisterEnum);
                }));
                return;
            }

            try
            {
                List<PdszDatabase.SwiAction> selectedSwiActions = GetSelectedSwiActions();
                List<PdszDatabase.SwiAction> linkedSwiActions = _programmingJobs.ProgrammingService.PdszDatabase.ReadLinkedSwiActions(selectedSwiActions, _programmingJobs.PsdzContext.Vehicle, null);
                ProgrammingJobs.OptionsItem topItemCurrent = null;
                int topIndexCurrent = checkedListBoxOptions.TopIndex;
                if (topIndexCurrent >= 0 && topIndexCurrent < checkedListBoxOptions.Items.Count)
                {
                    topItemCurrent = checkedListBoxOptions.Items[topIndexCurrent] as ProgrammingJobs.OptionsItem;
                }

                _ignoreCheck = true;
                checkedListBoxOptions.BeginUpdate();
                checkedListBoxOptions.Items.Clear();
                if (_optionsDict != null && _programmingJobs.SelectedOptions != null && swiRegisterEnum.HasValue)
                {
                    if (_optionsDict.TryGetValue(swiRegisterEnum.Value, out List<ProgrammingJobs.OptionsItem> optionsItems))
                    {
                        foreach (ProgrammingJobs.OptionsItem optionsItem in optionsItems)
                        {
                            CheckState checkState = CheckState.Unchecked;
                            bool addItem = true;
                            int selectIndex = _programmingJobs.SelectedOptions.IndexOf(optionsItem);
                            if (selectIndex >= 0)
                            {
                                if (selectIndex == _programmingJobs.SelectedOptions.Count - 1)
                                {
                                    checkState = CheckState.Checked;
                                }
                                else
                                {
                                    checkState = CheckState.Indeterminate;
                                }
                            }
                            else
                            {
                                if (linkedSwiActions != null &&
                                    linkedSwiActions.Any(x => string.Compare(x.Id, optionsItem.SwiAction.Id, StringComparison.OrdinalIgnoreCase) == 0))
                                {
                                    addItem = false;
                                }
                                else
                                {
                                    if (!_programmingJobs.ProgrammingService.PdszDatabase.EvaluateXepRulesById(optionsItem.SwiAction.Id, _programmingJobs.PsdzContext.Vehicle, null))
                                    {
                                        addItem = false;
                                    }
                                }
                            }

                            if (addItem)
                            {
                                checkedListBoxOptions.Items.Add(optionsItem, checkState);
                            }
                        }
                    }
                }

                if (topItemCurrent != null)
                {
                    int topIndexNew = checkedListBoxOptions.Items.IndexOf(topItemCurrent);
                    if (topIndexNew >= 0 && topIndexNew < checkedListBoxOptions.Items.Count)
                    {
                        checkedListBoxOptions.TopIndex = topIndexNew;
                    }
                }
            }
            finally
            {
                checkedListBoxOptions.EndUpdate();
                _ignoreCheck = false;
            }
        }

        private List<PdszDatabase.SwiAction> GetSelectedSwiActions()
        {
            if (_programmingJobs.PsdzContext == null || _programmingJobs.SelectedOptions == null)
            {
                return null;
            }

            List<PdszDatabase.SwiAction> selectedSwiActions = new List<PdszDatabase.SwiAction>();
            foreach (ProgrammingJobs.OptionsItem optionsItem in _programmingJobs.SelectedOptions)
            {
                if (optionsItem.SwiAction != null)
                {
                    log.InfoFormat("GetSelectedSwiActions Selected: {0}", optionsItem.SwiAction);
                    selectedSwiActions.Add(optionsItem.SwiAction);
                }
            }

            log.InfoFormat("GetSelectedSwiActions Count: {0}", selectedSwiActions.Count);

            return selectedSwiActions;
        }

        private void UpdateTargetFa(bool reset = false)
        {
            _programmingJobs.UpdateTargetFa(reset);
            UpdateCurrentOptions();
        }

        private async Task<bool> StartProgrammingServiceTask(string istaFolder)
        {
            return await Task.Run(() => _programmingJobs.StartProgrammingService(_cts, istaFolder)).ConfigureAwait(false);
        }

        private async Task<bool> StopProgrammingServiceTask()
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return await Task.Run(() => _programmingJobs.StopProgrammingService(_cts)).ConfigureAwait(false);
        }

        private async Task<List<EdInterfaceEnet.EnetConnection>> SearchVehiclesTask()
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return await Task.Run(() => SearchVehicles()).ConfigureAwait(false);
        }

        private List<EdInterfaceEnet.EnetConnection> SearchVehicles()
        {
            List<EdInterfaceEnet.EnetConnection> detectedVehicles;
            using (EdInterfaceEnet edInterface = new EdInterfaceEnet(false))
            {
                detectedVehicles = edInterface.DetectedVehicles("auto:all");
            }

            return detectedVehicles;
        }

        private async Task<bool> ConnectVehicleTask(string istaFolder, string remoteHost, bool useIcom)
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return await Task.Run(() => _programmingJobs.ConnectVehicle(_cts, istaFolder, remoteHost, useIcom)).ConfigureAwait(false);
        }

        private async Task<bool> DisconnectVehicleTask()
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            return await Task.Run(() => _programmingJobs.DisconnectVehicle(_cts)).ConfigureAwait(false);
        }

        private async Task<bool> VehicleFunctionsTask(ProgrammingJobs.OperationType operationType)
        {
            return await Task.Run(() => _programmingJobs.VehicleFunctions(_cts, operationType)).ConfigureAwait(false);
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonAbort_Click(object sender, EventArgs e)
        {
            if (TaskActive)
            {
                _cts?.Cancel();
            }
        }

        private void buttonIstaFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialogIsta.SelectedPath = textBoxIstaFolder.Text;
            DialogResult result = folderBrowserDialogIsta.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxIstaFolder.Text = folderBrowserDialogIsta.SelectedPath;
                UpdateDisplay();
            }
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            UpdateDisplay();
            StoreSettings();
            timerUpdate.Enabled = false;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            _ignoreChange = true;
            comboBoxLanguage.BeginUpdate();
            comboBoxLanguage.Items.Clear();
            PropertyInfo[] langueProperties = typeof(PdszDatabase.EcuTranslation).GetProperties();
            foreach (PropertyInfo propertyInfo in langueProperties)
            {
                string name = propertyInfo.Name;
                if (name.StartsWith("Text", StringComparison.OrdinalIgnoreCase))
                {
                    comboBoxLanguage.Items.Add(name.Substring(4));
                }
            }

            comboBoxLanguage.SelectedIndex = 0;
            comboBoxLanguage.EndUpdate();
            _ignoreChange = false;

            LoadSettings();
            UpdateDisplay();
            UpdateStatus();
            timerUpdate.Enabled = true;
            labelProgressEvent.Text = string.Empty;
        }

        private void timerUpdate_Tick(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void buttonStartHost_Click(object sender, EventArgs e)
        {
            if (TaskActive)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            StartProgrammingServiceTask(textBoxIstaFolder.Text).ContinueWith(task =>
            {
                TaskActive = false;
                _cts.Dispose();
                _cts = null;
            });

            TaskActive = true;
            UpdateDisplay();
        }

        private void buttonStopHost_Click(object sender, EventArgs e)
        {
            if (TaskActive)
            {
                return;
            }

            StopProgrammingServiceTask().ContinueWith(task =>
            {
                TaskActive = false;
                if (e == null)
                {
                    BeginInvoke((Action)(() =>
                    {
                        Close();
                    }));
                }
            });

            TaskActive = true;
            UpdateDisplay();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (TaskActive)
            {
                e.Cancel = true;
                return;
            }

            if (_programmingJobs.ProgrammingService != null && _programmingJobs.ProgrammingService.IsPsdzPsdzServiceHostInitialized())
            {
                buttonStopHost_Click(sender, null);
                e.Cancel = true;
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (TaskActive)
            {
                return;
            }

            if (_programmingJobs.PsdzContext?.Connection != null)
            {
                return;
            }

            bool useIcom = checkBoxIcom.Checked;
            _cts = new CancellationTokenSource();
            ConnectVehicleTask(textBoxIstaFolder.Text, ipAddressControlVehicleIp.Text, useIcom).ContinueWith(task =>
            {
                TaskActive = false;
                _cts.Dispose();
                _cts = null;
            });

            TaskActive = true;
            UpdateDisplay();
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            if (TaskActive)
            {
                return;
            }

            if (_programmingJobs.PsdzContext?.Connection == null)
            {
                return;
            }

            DisconnectVehicleTask().ContinueWith(task =>
            {
                TaskActive = false;
            });

            TaskActive = true;
            UpdateDisplay();
        }

        private void buttonFunc_Click(object sender, EventArgs e)
        {
            if (TaskActive)
            {
                return;
            }

            if (_programmingJobs.PsdzContext?.Connection == null)
            {
                return;
            }

            ProgrammingJobs.OperationType operationType = ProgrammingJobs.OperationType.CreateOptions;
            if (sender == buttonCreateOptions)
            {
                operationType = ProgrammingJobs.OperationType.CreateOptions;
            }
            else if (sender == buttonModILevel)
            {
                operationType = ProgrammingJobs.OperationType.BuildTalILevel;
                UpdateTargetFa(true);
            }
            else if (sender == buttonModFa)
            {
                operationType = ProgrammingJobs.OperationType.BuildTalModFa;
                UpdateTargetFa();
            }
            else if (sender == buttonExecuteTal)
            {
                operationType = ProgrammingJobs.OperationType.ExecuteTal;
            }

            _cts = new CancellationTokenSource();
            VehicleFunctionsTask(operationType).ContinueWith(task =>
            {
                TaskActive = false;
                _cts.Dispose();
                _cts = null;
            });

            TaskActive = true;
            UpdateDisplay();
        }

        private void buttonVehicleSearch_Click(object sender, EventArgs e)
        {
            if (TaskActive)
            {
                return;
            }

            bool preferIcom = checkBoxIcom.Checked;
            SearchVehiclesTask().ContinueWith(task =>
            {
                TaskActive = false;
                BeginInvoke((Action)(() =>
                {
                    List<EdInterfaceEnet.EnetConnection> detectedVehicles = task.Result;
                    EdInterfaceEnet.EnetConnection connectionDirect = null;
                    EdInterfaceEnet.EnetConnection connectionIcom = null;
                    EdInterfaceEnet.EnetConnection connectionSelected = null;
                    if (detectedVehicles != null)
                    {
                        foreach (EdInterfaceEnet.EnetConnection enetConnection in detectedVehicles)
                        {
                            if (enetConnection.IpAddress.ToString().StartsWith("192.168.11."))
                            {   // ICOM vehicle IP
                                continue;
                            }

                            if (connectionSelected == null)
                            {
                                connectionSelected = enetConnection;
                            }

                            switch (enetConnection.ConnectionType)
                            {
                                case EdInterfaceEnet.EnetConnection.InterfaceType.Icom:
                                    if (connectionIcom == null)
                                    {
                                        connectionIcom = enetConnection;
                                    }
                                    break;

                                default:
                                    if (connectionDirect == null)
                                    {
                                        connectionDirect = enetConnection;
                                    }
                                    break;
                            }
                        }
                    }

                    if (preferIcom)
                    {
                        if (connectionIcom != null)
                        {
                            connectionSelected = connectionIcom;
                        }
                    }
                    else
                    {
                        if (connectionDirect != null)
                        {
                            connectionSelected = connectionDirect;
                        }
                    }

                    bool ipValid = false;
                    try
                    {
                        if (connectionSelected != null)
                        {
                            ipAddressControlVehicleIp.Text = connectionSelected.IpAddress.ToString();
                            checkBoxIcom.Checked = connectionSelected.ConnectionType == EdInterfaceEnet.EnetConnection.InterfaceType.Icom;
                            ipValid = true;
                        }
                    }
                    catch (Exception)
                    {
                        ipValid = false;
                    }

                    if (!ipValid)
                    {
                        ipAddressControlVehicleIp.Text = DefaultIp;
                        checkBoxIcom.Checked = false;
                    }
                }));

            });

            TaskActive = true;
            UpdateDisplay();
        }

        private void checkedListBoxOptions_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_ignoreCheck)
            {
                return;
            }

            if (e.Index >= 0 && e.Index < checkedListBoxOptions.Items.Count)
            {
                if (checkedListBoxOptions.Items[e.Index] is ProgrammingJobs.OptionsItem optionsItem)
                {
                    if (e.CurrentValue == CheckState.Indeterminate)
                    {
                        e.NewValue = e.CurrentValue;
                    }
                    else
                    {
                        if (_programmingJobs.SelectedOptions != null)
                        {
                            if (e.NewValue == CheckState.Checked)
                            {
                                if (!_programmingJobs.SelectedOptions.Contains(optionsItem))
                                {
                                    _programmingJobs.SelectedOptions.Add(optionsItem);
                                }
                            }
                            else
                            {
                                _programmingJobs.SelectedOptions.Remove(optionsItem);
                            }
                        }
                    }
                }
            }

            BeginInvoke((Action)(() =>
            {
                UpdateTargetFa();
            }));
        }

        private void comboBoxOptionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreChange)
            {
                return;
            }

            BeginInvoke((Action)(() =>
            {
                UpdateTargetFa();
            }));
        }

        private void comboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreChange)
            {
                return;
            }

            _programmingJobs.ClientContext.Language = comboBoxLanguage.SelectedItem.ToString();

            BeginInvoke((Action)(() =>
            {
                UpdateCurrentOptions();
            }));
        }
    }
}
