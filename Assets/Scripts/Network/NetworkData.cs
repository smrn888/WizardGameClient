using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks; 
using System.Collections.Generic;
using SocketIOClient;
using Newtonsoft.Json;
using SocketIOClient.Newtonsoft.Json; 

// ================== Data Classes ==================
// NOTE: PlayerData is now defined in PlayerData.cs only

[Serializable]
public class NetworkLoginRequest
{
    public string username;
    public string password;
}

[Serializable]
public class NetworkRegisterRequest
{
    public string username;
    public string email;
    public string password;
}

[Serializable]
public class AuthResponse
{
    public string token;
    public string playerId;
}

// ❌ REMOVED: Duplicate PlayerData class
// The full PlayerData class is in PlayerData.cs

[Serializable]
public class PositionUpdateData
{
    public string playerId;
    public PositionData position;
}

[Serializable]
public class PositionData
{
    public float x;
    public float y;
    
    public PositionData(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, 0);
    }
}

[Serializable]
public class DirectionData
{
    public float x;
    public float y;
    
    public DirectionData(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    
    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
}

[Serializable]
public class ColorData
{
    public float r;
    public float g;
    public float b;
    public float a;
    
    public ColorData(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }
    
    public Color ToColor()
    {
        return new Color(r, g, b, a);
    }
}

[Serializable]
public class SpellCastData
{
    public string casterId;
    public string casterName;
    public string spellName;
    public PositionData position;
    public DirectionData direction;
    public ColorData color;
    public int damage;
    public float speed;
}

[Serializable]
public class DamageReceivedData
{
    public string attackerId;
    public string targetId;
    public float damage;
    public string source;
}

[Serializable]
public class PlayerDeathData
{
    public string playerId;
    public string killerId;
}

[Serializable]
public class CombatStatus
{
    public int activeCombats;
    public int totalKills;
    public int totalDeaths;
}

[Serializable]
public class ServerInfo
{
    public string version;
    public int onlinePlayers;
    public int totalPlayers;
    public bool maintenanceMode;
    public string message;
}

// ✅ Added: Session data class for SaveManager
[Serializable]
public class SessionData
{
    public string token;
    public string playerId;
}

[Serializable]
public class ExtendedAuthResponse
{
    public bool success;
    public string message;
    public string token;
    public string playerId;
    public string username;
    public string house;
    public bool needsSorting;
}

// ❌ REMOVED: Duplicate definitions of ActivePlayersResponse and PlayerPositionData
// These classes are already defined elsewhere in your project
// Search your codebase or move them here if needed