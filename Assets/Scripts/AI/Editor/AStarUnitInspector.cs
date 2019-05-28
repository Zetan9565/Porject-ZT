﻿using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AStarUnit))]
[CanEditMultipleObjects]
public class AStarUnitInspector : Editor
{
    new SerializedProperty target;
    SerializedProperty unitSize;
    SerializedProperty targetFootOffset;
    SerializedProperty targetFollowStartDistance;
    SerializedProperty footOffset;
    SerializedProperty fixedOffset;
    SerializedProperty moveMode;
    SerializedProperty rigidbody;
    SerializedProperty rigidbody2D;
    SerializedProperty controller;
    SerializedProperty turnSpeed;
    SerializedProperty moveSpeed;
    SerializedProperty slopeLimit;
    SerializedProperty stopDistance;
    SerializedProperty autoRepath;
    SerializedProperty drawGizmos;
    SerializedProperty pathRenderer;

    AStarManager manager;

    private void OnEnable()
    {
        unitSize = serializedObject.FindProperty("unitSize");
        target = serializedObject.FindProperty("target");
        targetFootOffset = serializedObject.FindProperty("targetFootOffset");
        targetFollowStartDistance = serializedObject.FindProperty("targetFollowStartDistance");
        footOffset = serializedObject.FindProperty("footOffset");
        fixedOffset = serializedObject.FindProperty("fixedOffset");
        moveMode = serializedObject.FindProperty("moveMode");
        rigidbody = serializedObject.FindProperty("rigidbody");
        rigidbody2D = serializedObject.FindProperty("rigidbody2D");
        controller = serializedObject.FindProperty("controller");
        turnSpeed = serializedObject.FindProperty("turnSpeed");
        moveSpeed = serializedObject.FindProperty("moveSpeed");
        slopeLimit = serializedObject.FindProperty("slopeLimit");
        stopDistance = serializedObject.FindProperty("stopDistance");
        autoRepath = serializedObject.FindProperty("autoRepath");
        drawGizmos = serializedObject.FindProperty("drawGizmos");
        pathRenderer = serializedObject.FindProperty("pathRenderer");
        manager = FindObjectOfType<AStarManager>();
    }

    public override void OnInspectorGUI()
    {
        if (manager) EditorGUILayout.LabelField("寻路单元格大小", manager.BaseCellSize.ToString());
        else EditorGUILayout.HelpBox("未找到A*对象!", MessageType.Warning);
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(unitSize, new GUIContent("单位大小"));
        if (unitSize.intValue < 1) unitSize.intValue = 1;
        if (manager) EditorGUILayout.LabelField("单位实际大小", (manager.BaseCellSize * unitSize.intValue).ToString());
        EditorGUILayout.PropertyField(footOffset, new GUIContent("自身脚部偏移"));
        EditorGUILayout.PropertyField(fixedOffset, new GUIContent("近似位置修正值"));
        if (fixedOffset.floatValue < 0) fixedOffset.floatValue = 0;
        EditorGUILayout.Space();
        if (Application.isPlaying)
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(target, new GUIContent("目标"));
            if (target.objectReferenceValue) EditorGUILayout.PropertyField(targetFootOffset, new GUIContent("目标脚部偏移"));
            GUI.enabled = true;
        }
        else
        {
            EditorGUILayout.PropertyField(target, new GUIContent("目标"));
            if (target.objectReferenceValue) EditorGUILayout.PropertyField(targetFootOffset, new GUIContent("目标脚部偏移"));
        }
        if (target.objectReferenceValue) EditorGUILayout.PropertyField(targetFollowStartDistance, new GUIContent("目标跟随距离修正值"));
        if (targetFollowStartDistance.floatValue < 0) targetFollowStartDistance.floatValue = 0;
        if (Application.isPlaying)
        {
            GUI.enabled = false;
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(moveMode, new GUIContent("移动方式"));
            GUI.enabled = true;
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(moveMode, new GUIContent("移动方式"));
        }
        if (moveMode.enumValueIndex == 1)
        {
            if (!manager)
            {
                if (!rigidbody2D.objectReferenceValue)
                {
                    EditorGUILayout.PropertyField(rigidbody, new GUIContent("刚体"));
                    if (rigidbody.objectReferenceValue)
                    {
                        EditorGUILayout.PropertyField(turnSpeed, new GUIContent("转向速度"));
                        EditorGUILayout.PropertyField(moveSpeed, new GUIContent("移动速度"));
                    }
                }
                if (!rigidbody.objectReferenceValue)
                {
                    EditorGUILayout.PropertyField(rigidbody2D, new GUIContent("2D 刚体"));
                    if (rigidbody2D.objectReferenceValue)
                        EditorGUILayout.PropertyField(moveSpeed, new GUIContent("移动速度"));
                }
            }
            else
            {
                if (manager.ThreeD)
                {
                    EditorGUILayout.PropertyField(rigidbody, new GUIContent("刚体"));
                    if (rigidbody.objectReferenceValue)
                    {
                        EditorGUILayout.PropertyField(turnSpeed, new GUIContent("转向速度"));
                        EditorGUILayout.PropertyField(moveSpeed, new GUIContent("移动速度"));
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(rigidbody2D, new GUIContent("2D 刚体"));
                    if (rigidbody2D.objectReferenceValue)
                        EditorGUILayout.PropertyField(moveSpeed, new GUIContent("移动速度"));
                }
            }
        }
        else if (moveMode.enumValueIndex == 2)
        {
            EditorGUILayout.PropertyField(controller, new GUIContent("控制器"));
            if (controller.objectReferenceValue)
            {
                EditorGUILayout.PropertyField(turnSpeed, new GUIContent("转向速度"));
                EditorGUILayout.PropertyField(moveSpeed, new GUIContent("移动速度"));
            }
        }
        else if (manager && manager.ThreeD || !manager)
        {
            EditorGUILayout.PropertyField(turnSpeed, new GUIContent("转向速度"));
            EditorGUILayout.PropertyField(moveSpeed, new GUIContent("移动速度"));
        }
        if (moveSpeed.floatValue < 0.01f) moveSpeed.floatValue = 0.01f;
        if (manager && manager.ThreeD || !manager)
            slopeLimit.floatValue = EditorGUILayout.Slider("最大移动坡度", slopeLimit.floatValue, 0, 90);
        EditorGUILayout.PropertyField(stopDistance, new GUIContent("提前停止距离"));
        if (stopDistance.floatValue < 0) stopDistance.floatValue = 0;
        EditorGUILayout.PropertyField(autoRepath, new GUIContent("自动修复路线"));
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(pathRenderer, new GUIContent("路线渲染器"));
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(drawGizmos, new GUIContent("绘制Gizmos"));
        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();
    }
}