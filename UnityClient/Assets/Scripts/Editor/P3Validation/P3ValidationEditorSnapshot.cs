using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class P3ValidationEditorSnapshot {
    public bool IsCompiling, IsUpdating, IsPlaying, IsPlayingOrWillChange, HasDirtyScenes, HasPrefabStage, PrefabStageDirty;
    public string ActiveScene, SelectionPath;
    public static P3ValidationEditorSnapshot Capture() {
        bool dirty=false; for(int i=0;i<EditorSceneManager.sceneCount;i++) dirty|=EditorSceneManager.GetSceneAt(i).isDirty;
        var prefab=PrefabStageUtility.GetCurrentPrefabStage();
        return new P3ValidationEditorSnapshot { IsCompiling=EditorApplication.isCompiling,IsUpdating=EditorApplication.isUpdating,IsPlaying=EditorApplication.isPlaying,IsPlayingOrWillChange=EditorApplication.isPlayingOrWillChangePlaymode,HasDirtyScenes=dirty,HasPrefabStage=prefab!=null,PrefabStageDirty=prefab!=null&&prefab.scene.isDirty,ActiveScene=EditorSceneManager.GetActiveScene().path,SelectionPath=Selection.activeObject?AssetDatabase.GetAssetPath(Selection.activeObject):null };
    }
}
