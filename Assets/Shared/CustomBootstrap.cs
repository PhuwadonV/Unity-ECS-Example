using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;

class CustomBootstrap : ICustomBootstrap
{
    public List<Type> Initialize(List<Type> systems)
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "CustomBootstrap":
                {
                    foreach (var system in systems)
                    {
                        Debug.Log(system.FullName);
                    }
                }
                break;
        }

        return systems;
    }
}