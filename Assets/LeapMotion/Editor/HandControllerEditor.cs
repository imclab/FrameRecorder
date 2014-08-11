using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(HandController))]
public class HandControllerEditor : Editor {

  private const float BOX_HEIGHT = 0.45f;
  private const float BOX_WIDTH = 0.965f;
  private const float BOX_DEPTH = 0.6671f;
  
  public void OnSceneGUI() {
    HandController controller = (HandController)target;
    Vector3 origin = controller.transform.TransformPoint(Vector3.zero);
    Vector3 top_left =
        controller.transform.TransformPoint(new Vector3(-BOX_WIDTH, BOX_HEIGHT, BOX_DEPTH));
    Vector3 top_right =
        controller.transform.TransformPoint(new Vector3(BOX_WIDTH, BOX_HEIGHT, BOX_DEPTH));
    Vector3 bottom_left =
        controller.transform.TransformPoint(new Vector3(-BOX_WIDTH, BOX_HEIGHT, -BOX_DEPTH));
    Vector3 bottom_right =
        controller.transform.TransformPoint(new Vector3(BOX_WIDTH, BOX_HEIGHT, -BOX_DEPTH));

    Handles.DrawLine(origin, top_left);
    Handles.DrawLine(origin, top_right);
    Handles.DrawLine(origin, bottom_left);
    Handles.DrawLine(origin, bottom_right);

    Handles.DrawLine(bottom_left, top_left);
    Handles.DrawLine(top_left, top_right);
    Handles.DrawLine(top_right, bottom_right);
    Handles.DrawLine(bottom_right, bottom_left);
  }

  public override void OnInspectorGUI() {
    HandController controller = (HandController)target;

    controller.separateLeftRight = EditorGUILayout.Toggle("Separate Left/Right",
                                                          controller.separateLeftRight);

    if (controller.separateLeftRight) {
      controller.leftGraphicsModel =
          (HandModel)EditorGUILayout.ObjectField("Left Hand Graphics Model",
                                                 controller.leftGraphicsModel,
                                                 typeof(HandModel), true);
      controller.rightGraphicsModel =
          (HandModel)EditorGUILayout.ObjectField("Right Hand Graphics Model",
                                                 controller.rightGraphicsModel,
                                                 typeof(HandModel), true);
      controller.leftPhysicsModel =
          (HandModel)EditorGUILayout.ObjectField("Left Hand Physics Model",
                                                 controller.leftPhysicsModel,
                                                 typeof(HandModel), true);
      controller.rightPhysicsModel =
          (HandModel)EditorGUILayout.ObjectField("Right Hand Physics Model",
                                                 controller.rightPhysicsModel,
                                                 typeof(HandModel), true);
    }
    else {
      controller.leftGraphicsModel = controller.rightGraphicsModel = 
          (HandModel)EditorGUILayout.ObjectField("Hand Graphics Model",
                                                 controller.leftGraphicsModel,
                                                 typeof(HandModel), true);

      controller.leftPhysicsModel = controller.rightPhysicsModel = 
          (HandModel)EditorGUILayout.ObjectField("Hand Physics Model",
                                                 controller.leftPhysicsModel,
                                                 typeof(HandModel), true);
    }

    controller.toolModel = 
        (ToolModel)EditorGUILayout.ObjectField("Tool Model",
                                               controller.toolModel,
                                               typeof(ToolModel), true);

    controller.handMovementScale =
        EditorGUILayout.Vector3Field("Hand Movement Scale", controller.handMovementScale);
    
    GUIStyle buttonStyle = new GUIStyle("Button");                   
    buttonStyle.alignment = TextAnchor.MiddleLeft;
    controller.recorderMode = (RecorderMode)EditorGUILayout.EnumPopup("Recorder Mode", controller.recorderMode);
    if (controller.recorderMode == RecorderMode.Record) {
      EditorGUILayout.BeginHorizontal();
      GUILayout.Label("File Path");
      if (GUILayout.Button(controller.recorderFilePath, buttonStyle)) {
        controller.recorderFilePath = EditorUtility.SaveFilePanel("Recorder File Path", "", "LeapRecording_" + System.DateTime.Now.ToString("yyyyMMdd_hhmm"), "bytes");
      }
      EditorGUILayout.EndHorizontal();
      controller.keyToRecord = (KeyCode)EditorGUILayout.EnumPopup("Key To Record", controller.keyToRecord);
      controller.keyToSave = (KeyCode)EditorGUILayout.EnumPopup("Key To Save", controller.keyToSave);
      controller.keyToReset = (KeyCode)EditorGUILayout.EnumPopup("Key To Reset", controller.keyToReset);      
    } else if (controller.recorderMode == RecorderMode.Playback) {
      controller.playerFilePath = (TextAsset)EditorGUILayout.ObjectField("File Path", controller.playerFilePath, typeof(TextAsset), true);
      controller.playerStartTime = EditorGUILayout.IntField("Start Time", controller.playerStartTime);
      controller.playerSpeed = EditorGUILayout.FloatField("Speed Multiplier", controller.playerSpeed);
      controller.playerLoop = EditorGUILayout.Toggle("Loop", controller.playerLoop);
      if (controller.playerLoop) {
        controller.playerDelay = EditorGUILayout.FloatField("Loop Delay", controller.playerDelay);
      }
    } 
    
    if (GUI.changed)
      EditorUtility.SetDirty(controller);

    Undo.RecordObject(controller, "Hand Preferences Changed: " + controller.name);
  }
}
