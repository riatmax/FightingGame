using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputManager
{
    public static readonly String[] controlSchemes = { "P1", "P2" };
    public static readonly Key[] debugAttackKeys = { Key.E, Key.RightShift }; // Temporary - not on the input actions so it stays off the arcade cabinet
    public static readonly String move = "Move";
    public static readonly String button1 = "Button 1";
    public static readonly String button2 = "Button 2";
    public static readonly String button3 = "Button 3";
}
