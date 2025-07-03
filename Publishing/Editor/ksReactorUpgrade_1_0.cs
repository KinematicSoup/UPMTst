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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Contains all methods required to update earlier version of Reactor to 1.0.0.</summary>
    public class ksReactorUpgrade_1_0
    {
        private List<string> m_changeLog = new List<string>();

        /// <summary>
        /// Applies updates when upgrading to Reactor 1.0.0. Converts serialized <see cref="ksColliderData"/> from the
        /// old format to the new component format on all prefab and instance entities.
        /// </summary>
        public void Upgrade()
        {
            // Process the prefabs
            foreach (ksBuildUtils.PrefabInfo prefabInfo in ksBuildUtils.IterateResourceAndAssetBundlePrefabs())
            {
                GameObject prefab = prefabInfo.GameObject;
                bool updated = false;

                // Update the prefab entities
                foreach (ksEntityComponent entity in prefab.GetComponentsInChildren<ksEntityComponent>())
                {
                    if (UpdateEntity(entity))
                    {
                        updated = true;
                        EditorUtility.SetDirty(entity);
                        m_changeLog.Add("- Updated entity prefab " + AssetDatabase.GetAssetPath(prefab) + ": " +
                            ksBuildUtils.FullGameObjectName(entity.gameObject));
                    }
                }

                if (updated)
                {
                    // Save the changes.
                    PrefabUtility.SavePrefabAsset(prefab);
                }
            }

            // Process instance entities
            foreach (Component component in ksBuildUtils.IterateSceneObjects(typeof(ksEntityComponent)))
            {
                ksEntityComponent entity = component as ksEntityComponent;
                if (entity != null && UpdateEntity(entity))
                {
                    EditorUtility.SetDirty(entity);
                    m_changeLog.Add("- Updated entity instance " + SceneManager.GetActiveScene().name + ": " +
                        ksBuildUtils.FullGameObjectName(entity.gameObject));
                }
            }

            ksLog.Info(this, "Reactor 1.0 updates complete\n" + string.Join("\n", m_changeLog));
        }

        // Suppress obsolete warnings
#pragma warning disable 0618
#pragma warning disable 0612
        /// <summary>
        /// Converts <see cref="ksColliderData"/> from the old format to the new component format on an entity.
        /// </summary>
        /// <param name="entity">Entity to update.</param>
        /// <returns>True if the entity was updated.</returns>
        private bool UpdateEntity(ksEntityComponent entity)
        {
            if (entity.ColliderData == null || entity.ColliderData.Count == 0)
            {
                return false;
            }
            foreach (ksColliderData.OldFormat oldData in entity.ColliderData)
            {
                ksColliderData colliderData = entity.gameObject.AddComponent<ksColliderData>();
                colliderData.Collider = oldData.Collider;
                colliderData.ContactOffset = oldData.ContactOffset;
                colliderData.Filter = oldData.Filter;
                colliderData.IsQuery = oldData.IsQuery;
                colliderData.IsSimulation = oldData.IsSimulation;
                colliderData.ShapeId = oldData.ShapeId;
                EditorUtility.SetDirty(colliderData);
            }
            entity.ColliderData.Clear();
            return true;
        }
#pragma warning restore 0612
#pragma warning restore 0618
    }
}
