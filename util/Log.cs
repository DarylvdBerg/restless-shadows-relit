using Godot;
using System;
using System.Diagnostics;

public static class Log
{
    [Conditional("DEBUG")]
    public static void Debug(string msg) => GD.Print($"[DEBUG] {msg}");
}
