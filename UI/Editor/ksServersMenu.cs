/*
KINEMATICSOUP CONFIDENTIAL
 Copyright(c) 2014-2024 KinematicSoup Technologies Incorporated 
 All Rights Reserved.

NOTICE:  All information contained herein is, and remains the property of 
KinematicSoup Technologies Incorporated and its suppliers, if any. The 
intellectual and technical concepts contained herein are proprietary to 
KinematicSoup Technologies Incorporated and its suppliers and may be covered by
U.S. and Foreign Patents, patents in process, and are protected by trade secret
or copyright law. Dissemination of this information or reproduction of this
material is strictly forbidden unless prior written permission is obtained from
KinematicSoup Technologies Incorporated.
*/

using KS.Unity.Editor;
using KS.Unity;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Menu for displaying the server list.</summary>
    public class ksServersMenu : ksAuthenticatedMenu
    {
        // Layout Constants
        private const int COLUMN_S = 50;
        private const int COLUMN_M = 100;
        private const int COLUMN_L = 150;
        private const int LABEL_WIDTH = 75;
        private const int FIELD_WIDTH = 175;
        private const int LINE_HEIGHT = 18;
        private const int PADDING = 4;
        private const int COL_PADDING = 20;

        // GUI Layout
        private static GUILayoutOption m_smallWidth = GUILayout.Width(COLUMN_S);
        private static GUILayoutOption m_mediumWidth = GUILayout.Width(COLUMN_M);
        private static GUILayoutOption m_largeWidth = GUILayout.Width(COLUMN_L);
        private static GUILayoutOption m_inputWidth = GUILayout.Width(LABEL_WIDTH + FIELD_WIDTH);
        private static GUILayoutOption m_buttonWidth = GUILayout.Width(FIELD_WIDTH);

        private ksPublishService m_service = null;
        private ksWindow m_window = null;
        private Vector2 m_scrollPosition = Vector2.zero;

        // Update UI
        private bool m_updateProjects = true;
        private bool m_updateImages = true;
        private bool m_updateServerForm = true;
        private bool m_updateServers = true;
        private bool m_updateVirtualMachines = true;

        private int m_imageIndex = -1;//-1 means select the bound image
        private int m_locationIndex = 0;
        private int m_sceneIndex = 0;
        private int m_roomIndex = 0;
        private uint m_selectedServer = 0;
        private bool m_keepAlive = false;
        private bool m_isPublic = true;
        private string m_name = "";

        private ksProjectInfo m_project = null;
        private List<ksProjectInfo> m_projects = new List<ksProjectInfo>();
        private List<ksPublishService.ImageInfo> m_GUIImages = new List<ksPublishService.ImageInfo>();

        private List<int> m_GUIImageIds = new List<int>();
        private List<string> m_GUILocationText = new List<string>();
        private List<string> m_GUIImageText = new List<string>();
        private List<string> m_GUISceneText = new List<string>();
        private List<string> m_GUIRoomText = new List<string>();
        private ksJSON m_GUIServers = null;

        private string m_layoutMessage = null;
        private MessageType m_layoutMessageType = MessageType.None;
        private string m_GUILayoutMessage = null;
        private MessageType m_GUILayoutMessageType = MessageType.None;
        private string m_serverFormMessage = null;
        private MessageType m_serverFormMessageType = MessageType.None;
        private string m_GUIServerFormMessage = null;
        private MessageType m_GUIServerFormMessageType = MessageType.None;

        /// <summary>Icon to display.</summary>
        public override Texture Icon
        {
            get { return ksTextures.Logo; }
        }

        /// <summary>Unity on enable</summary>
        private void OnEnable()
        {
            SetLoginMenu(typeof(ksLoginMenu));
            hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>Called when the menu is opened.</summary>
        /// <param name="window">GUI window</param>
        public override void OnOpen(ksWindow window)
        {
            m_window = window;
            m_service = ksPublishService.Get();
            m_service.OnUpdateProjects += OnUpdateProjects;
            m_service.OnSelectProject += OnSelectProject;
            m_service.OnUpdateImages += OnUpdateImages;
            m_service.OnStartServer += OnServerAction;
            m_service.OnStopServer += OnServerAction;
            m_service.OnUpdateServers += OnUpdateSevers;
            m_service.OnUpdateVirtualMachines += OnUpdateVirtualMachines;
            m_service.OnBindImage += OnBindImage;
            m_service.Refresh();
        }

        /// <summary>Called when the menu is closed.</summary>
        /// <param name="window">GUI window</param>
        public override void OnClose(ksWindow window)
        {
            m_service.OnUpdateProjects -= OnUpdateProjects;
            m_service.OnSelectProject -= OnSelectProject;
            m_service.OnUpdateImages -= OnUpdateImages;
            m_service.OnStartServer -= OnServerAction;
            m_service.OnStopServer -= OnServerAction;
            m_service.OnUpdateServers -= OnUpdateSevers;
            m_service.OnUpdateVirtualMachines -= OnUpdateVirtualMachines;
            m_service.OnBindImage -= OnBindImage;
        }

        /// <summary>Draw the GUI.</summary>
        /// <param name="window">GUI window</param>
        public override void OnDraw(ksWindow window)
        {
            // Check compiling
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox("Compiling scripts...", MessageType.Warning);
                ClearStates();
                return;
            }

            // Update GUI
            CheckLayout();
            ksStyle.ProjectSelector(ksEditorWebService.Email, m_project, m_projects, Logout, SelectProject);
            if (!string.IsNullOrEmpty(m_GUILayoutMessage))
            {
                EditorGUILayout.HelpBox(m_GUILayoutMessage, m_GUILayoutMessageType);
                return;
            }

            DrawSelectImageForm();
            DrawServerList();
        }

        /// <summary>Logout of the service and reset the menu.</summary>
        private void Logout()
        {
            ksEditorWebService.Logout(ksReactorConfig.Instance.Urls.Publishing + "/v3/logout");
            m_window.Repaint();
        }

        /// <summary>Select a project from the project list.</summary>
        /// <param name="project">Selected project</param>
        private void SelectProject(ksProjectInfo project)
        {
            m_project = project;
            m_service.SelectedProjectId = m_project.Id;
        }

        /// <summary>Draw image selection form.</summary>
        private void DrawSelectImageForm()
        {
            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            // Rooms
            EditorGUILayout.LabelField("Select Image", EditorStyles.boldLabel);
            m_locationIndex = EditorGUILayout.Popup("Location:", m_locationIndex, m_GUILocationText.ToArray(), m_inputWidth);
            m_imageIndex = EditorGUILayout.Popup("Image:", m_imageIndex, m_GUIImageText.ToArray(), m_inputWidth);
            m_sceneIndex = EditorGUILayout.Popup("Scene:", m_sceneIndex, m_GUISceneText.ToArray(), m_inputWidth);
            m_roomIndex = EditorGUILayout.Popup("Room:", m_roomIndex, m_GUIRoomText.ToArray(), m_inputWidth);
            m_keepAlive = EditorGUILayout.Toggle("Keep Alive:", m_keepAlive, m_inputWidth);
            m_isPublic = EditorGUILayout.Toggle("Is Public:", m_isPublic, m_inputWidth);
            m_name = EditorGUILayout.TextField("Name:", m_name, m_inputWidth);

            // Launch Button
            GUILayout.BeginHorizontal();
            GUILayout.Space(LABEL_WIDTH + PADDING);
            if (GUILayout.Button("Launch Server", m_buttonWidth))
            {
                string image = m_GUIImageIds[m_imageIndex].ToString();
                string scene = m_GUISceneText[m_sceneIndex];
                string room = m_GUIRoomText[m_roomIndex];
                string location = m_GUILocationText[m_locationIndex];
                if (location.EndsWith(" (overloaded)"))
                {
                    location = location.Substring(0, location.Length - " (overloaded)".Length);
                }
                m_service.StartServer(image, scene, room, m_name, location, m_keepAlive, m_isPublic);
                m_serverFormMessage = "Starting server...";
                m_serverFormMessageType = MessageType.Info;
                m_updateServerForm = true;
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(m_GUIServerFormMessage))
            {
                EditorGUILayout.HelpBox(m_GUIServerFormMessage, m_GUIServerFormMessageType);
                return;
            }
        }

        /// <summary>Draw the list of servers.</summary>
        private void DrawServerList()
        {
            // List Header
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Id", EditorStyles.boldLabel, m_smallWidth);
            GUILayout.Space(COL_PADDING);
            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, m_largeWidth);
            GUILayout.Space(COL_PADDING);
            EditorGUILayout.LabelField("Image", EditorStyles.boldLabel, m_mediumWidth);
            GUILayout.Space(COL_PADDING);
            EditorGUILayout.LabelField("Creator", EditorStyles.boldLabel, m_mediumWidth);
            GUILayout.Space(COL_PADDING);
            EditorGUILayout.LabelField("State", EditorStyles.boldLabel, m_mediumWidth);
            GUILayout.Space(COL_PADDING);
            EditorGUILayout.LabelField("Action", EditorStyles.boldLabel, m_mediumWidth);
            EditorGUILayout.EndHorizontal();

            // Server List
            if (m_GUIServers != null && m_GUIServers.Count > 0)
            {
                foreach (ksJSON server in m_GUIServers.Array)
                {
                    Rect row = EditorGUILayout.BeginHorizontal(GUILayout.Height(LINE_HEIGHT));
                    Rect expandArea = row;
                    expandArea.width = COLUMN_S + 2 * COLUMN_M + COLUMN_L + PADDING + 3 * COL_PADDING;
                    if (GUI.Button(expandArea, GUIContent.none, GUI.skin.label))
                    {
                        if (m_selectedServer == server["id"])
                        {
                            m_selectedServer = 0;
                        }
                        else
                        {
                            m_selectedServer = server["id"];
                        }
                    }

                    EditorStyles.foldout.fixedWidth = COLUMN_S;
                    GUILayout.Space(PADDING);
                    EditorGUILayout.Foldout(m_selectedServer == server["id"], server["id"]);
                    GUILayout.Space(COL_PADDING);
                    EditorGUILayout.LabelField(server["name"], m_largeWidth);
                    GUILayout.Space(COL_PADDING);
                    EditorGUILayout.LabelField(server["imageName"] + " (" + server["imageVersion"] + ")", m_mediumWidth);
                    GUILayout.Space(COL_PADDING);
                    EditorGUILayout.LabelField(server["userAlias"], m_mediumWidth);
                    GUILayout.Space(COL_PADDING);
                    EditorGUILayout.LabelField((server.HasField("state") ? server["state"] : "running"), m_mediumWidth);
                    GUILayout.Space(COL_PADDING);

                    if (server["canStop"].Bool)
                    {
                        if (EditorGUILayout.DropdownButton(new GUIContent("Action"), FocusType.Keyboard, m_mediumWidth, GUILayout.Height(16)))
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Stop"), false, StopServer, server);
                            menu.AddItem(new GUIContent("Restart"), false, RestartServer, server);
                            menu.AddItem(new GUIContent("Duplicate"), false, StartServer, server);
                            menu.ShowAsContext();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (m_selectedServer == server["id"])
                    {
                        row.height = 140.0f;
                        row.yMin -= 3;
                        EditorGUI.DrawRect(row, new Color32(0, 0, 0, 32));
                        PrintRoomData("Uptime:", server["uptime"]);
                        PrintRoomData("Scene:", server["scene"]);
                        PrintRoomData("Room:", server["room"]);
                        PrintRoomData("Location:", server["location"]);
                        PrintRoomData("Host:", server["ip"]);
                        PrintRoomData("Port:", server["port"]);
                        EditorGUILayout.Space();
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>Gets the image ID from an image name and version.</summary>
        /// <param name="name">Image name</param>
        /// <param name="version">Image version</param>
        /// <returns>Image ID, or -1 if not found.</returns>
        private int GetImageId(string name, string version)
        {
            List<ksPublishService.ImageInfo> images = m_service.Images;
            foreach (ksPublishService.ImageInfo image in images)
            {
                if (image.Name == name && image.Version == version)
                {
                    return image.Id;
                }
            }
            return -1;
        }

        /// <summary>Indent and draw labels for a room info value.</summary>
        /// <param name="label">Label</param>
        /// <param name="value">Value</param>
        private void PrintRoomData(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(COL_PADDING);
            EditorGUILayout.PrefixLabel(label, EditorStyles.label, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(value, GUILayout.Height(16));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Stop a server.</summary>
        /// <param name="value">Server data of type <see cref="ksJSON"/>.</param>
        private void StopServer(object value)
        {
            ksJSON server = value as ksJSON;
            m_service.StopServer(server["id"]);
            m_serverFormMessage = "Stopping server...";
            m_serverFormMessageType = MessageType.Info;
            m_updateServerForm = true;
        }

        /// <summary>Stop and restart a server.</summary>
        /// <param name="value">Server data of type <see cref="ksJSON"/>.</param>
        private void RestartServer(object value)
        {
            ksJSON server = value as ksJSON;
            m_service.StopServer(server["id"]);
            m_service.StartServer(
                GetImageId(server["imageName"], server["imageVersion"]).ToString(), 
                server["scene"], 
                server["room"], 
                server["name"],
                server["location"], 
                server["keepAlive"],
                server["isPublic"]
            );
            m_serverFormMessage = "Restarting server...";
            m_serverFormMessageType = MessageType.Info;
            m_updateServerForm = true;
        }

        /// <summary>Start a server.</summary>
        /// <param name="value">Server data of type <see cref="ksJSON"/>.</param>
        private void StartServer(object value)
        {
            ksJSON server = value as ksJSON;
            m_service.StartServer(
                GetImageId(server["imageName"], server["imageVersion"]).ToString(),
                server["scene"], 
                server["room"], 
                server["name"],
                server["location"], 
                server["keepAlive"],
                server["isPublic"]
            );
            m_serverFormMessage = "Starting server...";
            m_serverFormMessageType = MessageType.Info;
            m_updateServerForm = true;
        }

        /// <summary>Handle project list updates.</summary>
        /// <param name="error">Error message</param>
        private void OnUpdateProjects(string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                m_layoutMessage = error;
                m_layoutMessageType = MessageType.Error;
            }
            else
            {
                m_layoutMessage = null;
                m_layoutMessageType = MessageType.None;
            }
            m_updateProjects = true;
            m_window.Repaint();
        }

        /// <summary>Handle project selection changes.</summary>
        /// <param name="selectedProjectId">Selected project ID.</param>
        /// <param name="error">Error message</param>
        private void OnSelectProject(int selectedProjectId, string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                m_layoutMessage = error;
                m_layoutMessageType = MessageType.Error;
            }
            else
            {
                m_layoutMessage = null;
                m_layoutMessageType = MessageType.None;
                m_project = null;
                foreach (ksProjectInfo project in m_projects)
                {
                    if (project.Id == m_service.SelectedProjectId)
                    {
                        m_project = project;
                    }
                }
                if (m_project == null)
                {
                    m_imageIndex = -1;
                    m_sceneIndex = 0;
                    m_roomIndex = 0;
                }
            }
            m_updateProjects = true;
            m_window.Repaint();
        }

        /// <summary>Handle image list updates.</summary>
        /// <param name="error">Error message</param>
        private void OnUpdateImages(string error)
        {
            m_updateImages = true;
            m_window.Repaint();
        }

        /// <summary>Handle server action requests.</summary>
        /// <param name="error">Error message</param>
        private void OnServerAction(string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                m_serverFormMessage = error;
                m_serverFormMessageType = MessageType.Error;
            }
            else
            {
                m_serverFormMessage = null;
                m_serverFormMessageType = MessageType.None;
            }
            m_updateServerForm = true;
            m_window.Repaint();
        }

        /// <summary>Handle server list updates.</summary>
        /// <param name="error">Error message</param>
        private void OnUpdateSevers(string error)
        {
            m_updateServers = true;
            m_window.Repaint();
        }

        /// <summary>Handle virtual machine list updates.</summary>
        /// <param name="error">Error message</param>
        private void OnUpdateVirtualMachines(string error)
        {
            m_updateVirtualMachines = true;
            m_window.Repaint();
        }

        /// <summary>Selects the bound image.</summary>
        /// <param name="imageId">Id of the image that was bound.</param>
        private void OnBindImage(int imageId)
        {
            int index = m_GUIImageIds.IndexOf(imageId);
            if (index >= 0)
            {
                m_imageIndex = index;
            }
        }

        /// <summary>Check and apply updates that affect the GUI layout.</summary>
        private void CheckLayout()
        {
            if (Event.current.type == EventType.Layout)
            {
                List<ksProjectInfo> projects = m_service.Projects;

                // Update layout message
                if (projects == null)
                {
                    m_GUILayoutMessage = "Fetching projects...";
                    m_GUILayoutMessageType = MessageType.Info;
                }
                else if (m_layoutMessage != null)
                {
                    m_GUILayoutMessage = m_layoutMessage;
                    m_GUILayoutMessageType = m_layoutMessageType;
                }
                else if (projects.Count == 0)
                {
                    m_GUILayoutMessage = "You are not an editor of any project. You must be added to a project " +
                            "before you can publish images.";
                    m_GUILayoutMessageType = MessageType.Warning;
                }
                else
                {
                    m_GUILayoutMessage = null;
                    m_GUILayoutMessageType = MessageType.None;
                }

                // Update project GUI data
                if (m_updateProjects)
                {
                    m_updateProjects = false;
                    m_projects = projects;
                    m_project = null;
                    foreach (ksProjectInfo project in m_projects)
                    {
                        if (project.Id == m_service.SelectedProjectId)
                        {
                            m_project = project;
                        }
                    }
                }

                // Update the image selection GUI data
                if (m_updateImages)
                {
                    UpdateImages();
                }

                // Update server form GUI data
                if (m_updateServerForm)
                {
                    m_updateServerForm = false;
                    m_GUIServerFormMessage = m_serverFormMessage;
                    m_GUIServerFormMessageType = m_serverFormMessageType;
                }

                // Update server list GUI data
                if (m_updateServers)
                {
                    m_updateServers = false;
                    m_GUIServers = m_service.Servers;
                }

                // Update virtual machines list GUI data
                if (m_updateVirtualMachines)
                {
                    // Track the old location so that we can update the selected location index
                    string oldLocation = "";
                    if (m_GUILocationText.Count > 0)
                    {
                        oldLocation = m_GUILocationText[m_locationIndex < m_GUILocationText.Count ? m_locationIndex : 0];
                        if (oldLocation.EndsWith(" (overloaded)"))
                        {
                            oldLocation = oldLocation.Substring(0, oldLocation.Length - " (overloaded)".Length);
                        }
                    }
                    m_locationIndex = 0;

                    m_GUILocationText.Clear();
                    m_updateVirtualMachines = false;
                    m_GUILocationText.Add("Any");
                    if (m_service.VirtualMachines != null)
                    {
                        // Add overloaded locations after other locations.  If the location matches the
                        // old selected location, then update the selected location index.
                        List<string> overloadedLocations = new List<string>();
                        foreach (ksJSON vm in m_service.VirtualMachines.Array)
                        {
                            string location = vm["location"];
                            if (vm.GetField("overloaded", 0) == 1)
                            {
                                overloadedLocations.Add(location);
                            }
                            else
                            {
                                m_GUILocationText.Add(location);
                                if (m_locationIndex == 0 && location == oldLocation)
                                {
                                    m_locationIndex = m_GUILocationText.Count - 1;
                                }
                            }
                        }

                        // Add overloaded locations at the bottom of the list
                        foreach (string overloadedLocation in overloadedLocations)
                        {
                            m_GUILocationText.Add(overloadedLocation + " (overloaded)");
                            if (m_locationIndex == 0 && overloadedLocation == oldLocation)
                            {
                                m_locationIndex = m_GUILocationText.Count - 1;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the id of the selected image. If not image is selected, returns the id of the bound image. Otherwise
        /// returns -1.
        /// </summary>
        /// <returns>Id of the selected or bound image.</returns>
        private int GetSelectedOrBoundImageId()
        {
            if (m_imageIndex >= 0 && m_imageIndex < m_GUIImageIds.Count)
            {
                return m_GUIImageIds[m_imageIndex];
            }
            // Try to get the image id to select from the image binding.
            if (!string.IsNullOrEmpty(ksReactorConfig.Instance.Build.ImageBinding))
            {
                string[] parts = ksReactorConfig.Instance.Build.ImageBinding.Split('.');
                int id;
                if (parts.Length < 3 || !int.TryParse(parts[2], out id))
                {
                    id = -1;
                }
                return id;
            }
            return -1;
        }

        /// <summary>Updates the images dropdown list.</summary>
        private void UpdateImages()
        {
            int selectedImageId = GetSelectedOrBoundImageId();
            m_imageIndex = -1;
            List<ksPublishService.ImageInfo> images = m_service.Images;
            m_GUIImageIds.Clear();
            m_GUIImageText.Clear();
            m_GUISceneText.Clear();
            m_GUIRoomText.Clear();

            if (images == null || images.Count == 0)
            {
                return;
            }

            int i = 0;
            foreach (ksPublishService.ImageInfo image in images)
            {
                m_GUIImageIds.Add(image.Id);
                m_GUIImageText.Add(image.Name + " (" + image.Version + ")");
                if (selectedImageId == image.Id)
                {
                    m_imageIndex = i;
                }
                i++;
            }
            if (m_imageIndex < 0)
            {
                m_imageIndex = 0;
            }
            int j = 0;
            foreach (KeyValuePair<string, List<string>> scene in images[m_imageIndex].Scenes)
            {
                m_GUISceneText.Add(scene.Key);
                if (m_sceneIndex == j++)
                {
                    foreach (string room in scene.Value)
                    {
                        m_GUIRoomText.Add(room);
                    }
                }
            }
        }

        /// <summary>Clear message and fetch states.</summary>
        private void ClearStates()
        {
            m_layoutMessage = null;
            m_layoutMessageType = MessageType.None;

            m_serverFormMessage = null;
            m_serverFormMessageType = MessageType.None;
        }
    }
}
