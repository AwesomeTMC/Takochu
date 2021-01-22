﻿using GL_EditorFramework;
using GL_EditorFramework.EditorDrawables;
using OpenTK.Graphics.ES10;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Takochu.fmt;
using Takochu.io;
using Takochu.smg;
using Takochu.smg.obj;
using static GL_EditorFramework.EditorDrawables.EditorSceneBase;
using static GL_EditorFramework.Framework;

namespace Takochu.ui
{
    public partial class EditorWindow : Form
    {
        public EditorWindow(string galaxyName)
        {
            InitializeComponent();
            mGalaxyName = galaxyName;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            scene = new EditorScene();
            cameraScene = new EditorScene();
            cameraScene.Visible = false;

            mGalaxy = Program.sGame.OpenGalaxy(mGalaxyName);

            // we load our scenario information here and will use it later
            foreach (BCSV.Entry entry in mGalaxy.mScenarioEntries)
            {
                int no = entry.Get<int>("ScenarioNo");
                TreeNode n = new TreeNode($"[{no}] {entry.Get<string>("ScenarioName")}")
                {
                    Tag = no
                };

                scenarioTreeView.Nodes.Add(n);
            }

            galaxyViewControl.MainDrawable = scene;
            galaxyViewControl.ActiveCamera = new GL_EditorFramework.StandardCameras.InspectCamera(1000f);
            galaxyViewControl.CameraDistance = 20.0f;

            scene.SelectionChanged += Scene_SelectionChanged;
            scene.ObjectsMoved += Scene_ObjectsMoved;
            scene.ListChanged += Scene_ListChanged;
            scene.ListEntered += Scene_ListEntered;
            scene.ListInvalidated += Scene_ListInvalidated;

            sceneListView.SelectionChanged += SceneListView_SelectionChanged;
            sceneListView.ItemsMoved += SceneListView_ItemsMoved;
            sceneListView.ListExited += SceneListView_ListExited;

            cameraListView.SelectionChanged += CameraListView_SelectionChanged;
            cameraListView.ItemsMoved += CameraSceneListView_ItemsMoved;
            cameraListView.ListExited += CameraSceneListView_ListExited;
            cameraScene.SelectionChanged += CameraScene_SelectionChanged;

            galaxyViewControl.CameraTarget = new OpenTK.Vector3(0, 0, 0);
            galaxyViewControl.Refresh();
        }


        private EditorScene scene;
        private EditorScene cameraScene;
        private string mGalaxyName;
        public int mCurrentScenario;

        private Galaxy mGalaxy;

        public void LoadScenario(int scenarioNo)
        {
            sceneListView.RootLists.Clear();
            cameraListView.RootLists.Clear();
            lightsTree.Nodes.Clear();

            scene.objects.Clear();
            cameraScene.objects.Clear();

            // we want to clear out the children of the 5 camera type root nodes
            //for (int i = 0; i < 5; i++)
            //    camerasTree.Nodes[i].Nodes.Clear();

            mGalaxy.SetScenario(scenarioNo);

            // first we need to get the proper layers that the galaxy itself uses
            List<string> layers = mGalaxy.GetGalaxyLayers(mGalaxy.GetMaskUsedInZoneOnCurrentScenario(mGalaxyName));

            // get our main galaxy's zone
            Zone mainZone = mGalaxy.GetZone(mGalaxyName);

            // now we get the zones used on these layers
            List<string> zonesUsed = new List<string>
            {
                // add our galaxy name itself so we can properly add it to a scene list with the other zones
                mGalaxyName
            };

            zonesUsed.AddRange(mainZone.GetZonesUsedOnLayers(layers));

            Dictionary<string, int> zoneMasks = new Dictionary<string, int>();

            List<AbstractObj> objects = new List<AbstractObj>();
            List<AbstractObj> areas = new List<AbstractObj>();
            List<AbstractObj> starts = new List<AbstractObj>();
            List<AbstractObj> mapparts = new List<AbstractObj>();

            Dictionary<string, List<Camera>> cameras = new Dictionary<string, List<Camera>>();
            List<Light> lights = new List<Light>();

            foreach (string zone in zonesUsed)
            {
                zoneMasks.Add(zone, mGalaxy.GetMaskUsedInZoneOnCurrentScenario(zone));

                Zone z = mGalaxy.GetZone(zone);

                objects.AddRange(z.GetObjectsFromLayers("Map", "Obj", mGalaxy.GetGalaxyLayers(zoneMasks[zone])));
                areas.AddRange(z.GetObjectsFromLayers("Map", "AreaObj", mGalaxy.GetGalaxyLayers(zoneMasks[zone])));
                starts.AddRange(z.GetObjectsFromLayers("Map", "StartObj", mGalaxy.GetGalaxyLayers(zoneMasks[zone])));
                mapparts.AddRange(z.GetObjectsFromLayers("Map", "MapPart", mGalaxy.GetGalaxyLayers(zoneMasks[zone])));

                cameras.Add(zone, z.mCameras);

                if (z.mLights != null)
                    lights.AddRange(z.mLights);

                if (!z.mIsMainGalaxy)
                {
                    Zone galaxyZone = mGalaxy.GetGalaxyZone();

                    // the first step
                    List<string> galaxyLayers = mGalaxy.GetGalaxyLayers(zoneMasks[mGalaxy.mName]);

                    foreach(string layer in galaxyLayers)
                    {
                        List<StageObj> stages = galaxyZone.mZones[layer];

                        foreach(StageObj o in stages)
                        {
                            if (o.mName == z.mZoneName)
                            {
                                objects.ForEach(obj =>
                                {
                                    if (obj.mParentZone.mZoneName == z.mZoneName)
                                    {
                                        obj.ApplyZoneOffset(o.mPosition, o.mRotation);
                                    }
                                });

                                areas.ForEach(obj =>
                                {
                                    if (obj.mParentZone.mZoneName == z.mZoneName)
                                    {
                                        obj.ApplyZoneOffset(o.mPosition, o.mRotation);
                                    }
                                });

                                starts.ForEach(obj =>
                                {
                                    if (obj.mParentZone.mZoneName == z.mZoneName)
                                    {
                                        obj.ApplyZoneOffset(o.mPosition, o.mRotation);
                                    }
                                });

                                mapparts.ForEach(obj =>
                                {
                                    if (obj.mParentZone.mZoneName == z.mZoneName)
                                    {
                                        obj.ApplyZoneOffset(o.mPosition, o.mRotation);
                                    }
                                });
                            } 
                        }
                    }
                }
            }

            sceneListView.RootLists.Add("Areas", areas);
            sceneListView.RootLists.Add("Objects", objects);
            sceneListView.RootLists.Add("Start", starts);
            sceneListView.RootLists.Add("Map Parts", mapparts);
            sceneListView.UpdateComboBoxItems();
            sceneListView.SelectedItems = scene.SelectedObjects;
            sceneListView.SetRootList("Areas");

            scene.objects.AddRange(areas);
            scene.objects.AddRange(objects);
            scene.objects.AddRange(starts);
            scene.objects.AddRange(mapparts);

            List<Camera> cubeCameras = new List<Camera>();
            List<Camera> groupCameras = new List<Camera>();
            List<Camera> eventCameras = new List<Camera>();
            List<Camera> startCameras = new List<Camera>();
            List<Camera> otherCameras = new List<Camera>();

            foreach (string zone in zonesUsed)
            {
                cameras[zone].ForEach(c =>
                {
                    if (c.GetCameraType() == Camera.CameraType.Cube)
                        cubeCameras.Add(c);
                });

                cameras[zone].ForEach(c =>
                {
                    if (c.GetCameraType() == Camera.CameraType.Group)
                        groupCameras.Add(c);
                });

                cameras[zone].ForEach(c =>
                {
                    if (c.GetCameraType() == Camera.CameraType.Event)
                        eventCameras.Add(c);
                });

                cameras[zone].ForEach(c =>
                {
                    if (c.GetCameraType() == Camera.CameraType.Start)
                        startCameras.Add(c);
                });

                cameras[zone].ForEach(c =>
                {
                    if (c.GetCameraType() == Camera.CameraType.Other)
                        otherCameras.Add(c);
                });
            }

            cameraListView.RootLists.Add("Cube", cubeCameras);
            cameraListView.RootLists.Add("Group", groupCameras);
            cameraListView.RootLists.Add("Event", eventCameras);
            cameraListView.RootLists.Add("Start", startCameras);
            cameraListView.RootLists.Add("Other", otherCameras);
            cameraListView.UpdateComboBoxItems();
            cameraListView.SelectedItems = cameraScene.SelectedObjects;
            cameraListView.SetRootList("Cube");

            cameraScene.objects.AddRange(cubeCameras);
            cameraScene.objects.AddRange(groupCameras);
            cameraScene.objects.AddRange(eventCameras);
            cameraScene.objects.AddRange(startCameras);
            cameraScene.objects.AddRange(otherCameras);

            lights.ForEach(l => lightsTree.Nodes.Add(l.ToString()));

            galaxyViewControl.Refresh();
        }


        private void scenarioTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            
        }

        private void scenarioTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (scenarioTreeView.SelectedNode != null)
            {
                mCurrentScenario = Convert.ToInt32(scenarioTreeView.SelectedNode.Tag);
                LoadScenario(mCurrentScenario);
            }
        }

        private void EditorWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            mGalaxy.Close();
        }

        private void EditorWindow_Load(object sender, EventArgs e)
        {

        }

        private void galaxyViewControl_Load(object sender, EventArgs e)
        {

        }

        private void Scene_ObjectsMoved(object sender, EventArgs e)
        {
            //update the property control because properties might have changed
            foreach(IObjectUIContainer objectUIContainer in objUIControl.ObjectUIContainers)
                objectUIContainer.UpdateProperties();

            objUIControl.Refresh();
        }

        private void Scene_SelectionChanged(object sender, EventArgs e)
        {
            //update sceneListView
            sceneListView.Refresh();

            if (scene.SelectedObjects.Count != 0)
            {
                // now we can jump to the object in the scene view list for easy access
                tabControl1.SelectedIndex = 1;

                // now let's get the type so we can jump to the right category
                // since you can select multiple objects, we will just jump to the first one
                SelectionSet set = scene.SelectedObjects;
                AbstractObj obj = set.ElementAt(0) as AbstractObj;

                switch (obj.mType)
                {
                    case "AreaObj":
                        sceneListView.SetRootList("Areas");
                        break;
                    case "Obj":
                        sceneListView.SetRootList("Objects");
                        break;
                    case "StartObj":
                        sceneListView.SetRootList("Start");
                        break;
                    case "MapPart":
                        sceneListView.SetRootList("Map Parts");
                        break;
                }
            }

            //fetch availible properties for selection
            scene.SetupObjectUIControl(objUIControl);
        }

        private void Scene_ListEntered(object sender, ListEventArgs e)
        {
            sceneListView.EnterList(e.List);
        }

        private void SceneListView_ListExited(object sender, ListEventArgs e)
        {
            scene.CurrentList = e.List;
            //fetch availible properties for list
            scene.SetupObjectUIControl(objUIControl);
        }

        private void SceneListView_ItemsMoved(object sender, ItemsMovedEventArgs e)
        {
            scene.ReorderObjects(sceneListView.CurrentList, e.OriginalIndex, e.Count, e.Offset);
            e.Handled = true;
            galaxyViewControl.Refresh();
        }

        private void Scene_ListChanged(object sender, GL_EditorFramework.EditorDrawables.ListChangedEventArgs e)
        {
            if (e.Lists.Contains(sceneListView.CurrentList))
            {
                sceneListView.UpdateAutoScrollHeight();
                sceneListView.Refresh();
            }
        }

        private void SceneListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //apply selection changes to scene
            if (e.SelectionChangeMode == SelectionChangeMode.SET)
            {
                scene.SelectedObjects.Clear();

                foreach (ISelectable obj in e.Items)
                    obj.SelectDefault(galaxyViewControl);
            }
            else if (e.SelectionChangeMode == SelectionChangeMode.ADD)
            {
                foreach (ISelectable obj in e.Items)
                    obj.SelectDefault(galaxyViewControl);
            }
            else //SelectionChangeMode.SUBTRACT
            {
                foreach (ISelectable obj in e.Items)
                    obj.DeselectAll(galaxyViewControl);
            }

            e.Handled = true;
            galaxyViewControl.Refresh();

            Scene_SelectionChanged(this, null);
        }

        private void Scene_ListInvalidated(object sender, ListEventArgs e)
        {
            if (sceneListView.CurrentList == e.List)
                sceneListView.InvalidateCurrentList();
        }

        private void sceneListView_ItemClicked(object sender, ItemClickedEventArgs e)
        {
            if (e.Clicks == 2 && e.Item is GL_EditorFramework.EditorDrawables.IEditableObject obj)
            {
                galaxyViewControl.CameraTarget = obj.GetFocusPoint();
            }
        }

        private void CameraListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.SelectionChangeMode == SelectionChangeMode.SET)
            {
                cameraScene.SelectedObjects.Clear();

                foreach (ISelectable obj in e.Items)
                    obj.SelectDefault(null);
            }

            e.Handled = true;

            CameraScene_SelectionChanged(this, null);
        }

        private void CameraSceneListView_ListExited(object sender, ListEventArgs e)
        {
            cameraScene.CurrentList = e.List;
            //fetch availible properties for list
            cameraScene.SetupObjectUIControl(cameraUIControl);
        }

        private void CameraSceneListView_ItemsMoved(object sender, ItemsMovedEventArgs e)
        {
            cameraScene.ReorderObjects(cameraListView.CurrentList, e.OriginalIndex, e.Count, e.Offset);
            e.Handled = true;
        }

        private void CameraScene_SelectionChanged(object sender, EventArgs e)
        {
            //update sceneListView
            cameraListView.Refresh();

            //fetch availible properties for selection
            cameraScene.SetupObjectUIControl(cameraUIControl);
        }

        private void CameraScene_ObjectsMoved(object sender, EventArgs e)
        {
            //update the property control because properties might have changed
            foreach (IObjectUIContainer objectUIContainer in cameraUIControl.ObjectUIContainers)
                objectUIContainer.UpdateProperties();

            cameraUIControl.Refresh();
        }
    }
}