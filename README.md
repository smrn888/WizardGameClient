## 📖 Overview

A real-time multiplayer wizard battle game inspired by Harry Potter, built with Unity and Node.js backend.

### 🎮 Key Features

- ✅ **Real-time Multiplayer** - Socket.IO integration for live battles
- ✅ **Authentication System** - JWT-based secure login
- ✅ **Spell Casting System** - 20+ unique spells with visual effects
- ✅ **House System** - Gryffindor, Slytherin, Ravenclaw, Hufflepuff
- ✅ **Inventory & Shop** - Buy wands, robes, and magical items
- ✅ **XP & Leveling System** - Progress and unlock new abilities
- ✅ **Combat System** - PvP battles with damage calculation
- ✅ **Quest System** - Story-driven missions

## 🛠️ Tech Stack

### Frontend (Unity)
- **Engine:** Unity 2022.3 LTS
- **Language:** C# 11.0
- **Networking:** Socket.IO Client
- **HTTP:** UnityWebRequest + Custom API Client
- **State Management:** Singleton Pattern

### Backend (Node.js)
- **Runtime:** Node.js 20.x
- **Framework:** Express.js
- **Database:** MongoDB (Mongoose ODM)
- **Real-time:** Socket.IO
- **Authentication:** JWT (JSON Web Tokens)
- **Security:** Helmet, CORS, Rate Limiting

### DevOps
- **Version Control:** Git & GitHub
- **Database Hosting:** MongoDB Atlas
- **Backend Hosting:** Railway / Render
- **Testing:** Unity Test Framework




## 🏗️ Architecture
```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│   Unity     │ ◄─────► │   Node.js   │ ◄─────► │  MongoDB    │
│   Client    │  HTTP   │   Server    │  Query  │   Database  │
│             │ Socket  │             │         │             │
└─────────────┘         └─────────────┘         └─────────────┘
```

### Client-Side Architecture
```
NetworkManager (Singleton)
├── APIClient (HTTP Requests)
├── Socket.IO (Real-time Events)
├── SaveManager (Local Persistence)
└── GameManager (Game State)
```

### Server-Side Architecture
```
Express Server
├── Auth Routes (JWT)
├── Game Routes (Player Data)
├── Shop Routes (Items)
└── Socket.IO Events (Real-time)
```

## 📊 Code Statistics
```
Language      Files    Lines    Code     Comments
─────────────────────────────────────────────────
C#              45     8,500    6,800      1,200
JavaScript      12     2,300    1,900        300
JSON             8       850      850          0
Markdown         3       450      450          0
─────────────────────────────────────────────────
Total           68    12,100    9,000      1,500
```

## 🔐 Security Features

- ✅ Password hashing (bcrypt)
- ✅ JWT token authentication
- ✅ Rate limiting on API endpoints
- ✅ Input validation and sanitization
- ✅ Helmet.js security headers
- ✅ CORS configuration

## 🚀 Performance Optimizations

- Object pooling for projectiles
- Efficient network sync (position updates every 100ms)
- Cached player controller references
- Async/await for all API calls
- Database indexing on playerId
- Socket.IO room-based events

## 📝 What I Learned

- Building scalable real-time multiplayer systems
- Implementing secure authentication flows
- Managing complex game state synchronization
- Optimizing network traffic for smooth gameplay
- Designing RESTful APIs with Express.js
- Working with NoSQL databases (MongoDB)
- Handling WebSocket connections at scale

## 🎓 Challenges Overcome

1. **Real-time Synchronization**
   - Problem: Players not seeing each other
   - Solution: Implemented proper Socket.IO event handlers and state management

2. **Authentication Flow**
   - Problem: Session persistence across scenes
   - Solution: Built custom SaveManager with local session storage

3. **Network Optimization**
   - Problem: High bandwidth usage
   - Solution: Implemented position update throttling and delta compression

## 📧 Contact

**Moein [Your Last Name]**
- 📧 Email: moeinrazavinabavi@gmail.com
- 💼 LinkedIn: https://linkedin.com/in/moein-razavi-nabavi

## 📜 License

This project is private and proprietary. All rights reserved.
