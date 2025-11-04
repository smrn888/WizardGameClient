/**
 * index.js - Enhanced Hogwarts Combat Arena
 * Features: Enemy pursuit through doors, Patronus AI, Spell collisions, Dementor attachment
 */

import MapRenderer from './MapRenderer.js';

const GAME_CONSTS = {
    PLAYER_SPEED: 4.0,
    PLAYER_RADIUS: 60,
    INITIAL_HEALTH: 100,
    INITIAL_LIVES: 3,

    SPELL_RANGE: 800,
    SPELL_RADIUS: 12,
    
    SPELLS: {
        Q: { name: 'Lumos', color: '#ffffff', damage: 10, speed: 5, cooldown: 50, type: 'light', power: 10, sound: 'lumos' },
        W: { name: 'Stupefy', color: '#ff0000', damage: 20, speed: 5.6, cooldown: 80, type: 'stun', power: 20, sound: 'stupefy' },
        E: { name: 'Expelliarmus', color: '#ff6600', damage: 25, speed: 6.3, cooldown: 120, type: 'disarm', power: 25, sound: 'expelliarmus' },
        R: { name: 'Avada Kedavra', color: '#00ff00', damage: 100, speed: 4.2, cooldown: 500, type: 'death', power: 100, sound: 'avada' },
        T: { name: 'Expecto Patronum', color: '#99ccff', damage: 0, speed: 0, cooldown: 800, type: 'patronus', power: 50, sound: 'patronum' }
    },

    ENEMY_FIRE_RATE: 2000,
    ENEMY_CHASE_SPEED: 2.0,
    DEMENTOR_CHASE_SPEED: 1.8,
    DEMENTOR_DRAIN_RATE: 5,
    DEMENTOR_ATTACH_DISTANCE: 50,
    ENEMY_RADIUS: 60,
    ENEMY_DAMAGE: 12,
    ENEMY_VISION_RANGE: 600,
    DOOR_DETECTION_RANGE: 150,
    PATRONUS_SEEK_SPEED: 3.5,
    PATRONUS_DAMAGE_RATE: 2,
};

class HogwartsGame {
    constructor(canvas) {
        this.musicTimeout = null;
        this.canvas = canvas;
        this.ctx = canvas.getContext('2d');
                // ... کدهای قبلی
                this.audioContext = null; 
                this.musicTimeout = null; // اگر این خط را قبلاً اضافه نکرده‌اید، اکنون اضافه کنید.
        
                // ✨ اضافه شدن نقشه فرکانس‌ها (freqMap) ✨
                this.freqMap = { 
                    // نت‌های A2 تا C5
                    'D2': 73.42, 'E2': 82.41, 'F2': 87.31, 'G2': 98.00, 'A2': 110.00, 'B2': 123.47,
                    'C3': 130.81, 'D3': 146.83, 'E3': 164.81, 'F3': 174.61, 'G3': 196.00, 'A3': 220.00, 'B3': 246.94,
                    'C4': 261.63, 'D4': 293.66, 'E4': 329.63, 'F4': 349.23, 'G4': 392.00, 'A4': 440.00, 'B4': 493.88,
                    'C5': 523.25, 'D5': 587.33, 'E5': 659.25,
                    
                    // نت‌های مورد نیاز برای موسیقی جدید
                    'A#3': 233.08, 'D#3': 155.56, 'D#4': 311.13,
                    
                    // اگر نت‌های بیشتری در موسیقی‌های دیگر استفاده کردید، اینجا اضافه کنید.
                }; 
        
        this.resizeCanvas = this.resizeCanvas.bind(this);
        this.resizeCanvas();
        window.addEventListener('resize', this.resizeCanvas);

        this.gameState = 'menu';
        this.mapRenderer = null;
        this.mapData = null;
        this.currentZone = null;

        this.player = this.initializePlayer();
        this.enemies = [];
        this.spells = [];
        this.particles = [];

        this.keys = {};
        this.mousePos = { x: 0, y: 0 };
        this.lastUpdateTime = performance.now();
        this.animationFrameId = null;
        this.score = 0;
        this.message = '';
        this.messageTimer = 0;

        this.lastSpellTime = {};
        this.lastEnemyFire = new Map();

        this.lastDementorSpawn = 0;
        this.dementorSpawnInterval = 30000;
        this.spellBeams = [];
        this.attachedDementors = new Map();
        this.audioContext = null;
        this.sounds = {};
        
        this.initAudio();
        this.loadMap();
        this.setupEventListeners();
    }

    initAudio() {
        try {
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            this.playBackgroundMusic();
        } catch (e) {
            console.warn('Audio not supported:', e);
        }
    }

    playBackgroundMusic() {
        if (!this.audioContext) return;
        const now = this.audioContext.currentTime;

        // --- نت‌های موسیقی برای یک حلقه ~۱ دقیقه‌ای ---
        // ' ' (فاصله) به معنای سکوت است.
        const mainMelody = [
            // Intro (Mysterious)
            'G3', ' ', 'A#3', ' ', 'C4', ' ', 'D4', ' ',
            'G3', ' ', 'F3', ' ', 'D#3', 'D3', 'C3', ' ',
            // Build-up
            'D4', 'D#4', 'F4', ' ', 'F4', 'G4', 'F4', 'D#4',
            'D4', 'C4', 'A#3', ' ', 'A#3', 'C4', 'D4', ' ',
            // Climax (Epic Theme)
            'G4', 'G4', 'G4', 'D#4', ' ', 'F4', 'F4', 'F4',
            'D4', ' ', 'D#4', 'D4', 'C4', 'A#3', 'G3', ' ',
            // Resolution
            'C4', ' ', 'D4', ' ', 'D#4', 'D4', 'C4', 'A#3',
            'A#3', 'G3', 'F3', ' ', 'G3', ' ', ' ', ' '
        ];

        const harmonyHorn = [
            'D3', ' ', 'F3', ' ', 'G3', ' ', 'A#3', ' ',
            'D3', ' ', 'C3', ' ', 'A#2', 'A2', 'G2', ' ',
            'A#3', 'C4', 'D4', ' ', 'D4', 'D#4', 'D4', 'C4',
            'A#3', 'G3', 'F3', ' ', 'F3', 'G3', 'A#3', ' ',
            'D4', 'D4', 'D4', 'A#3', ' ', 'C4', 'C4', 'C4',
            'A#3', ' ', 'A#3', 'A3', 'G3', 'F3', 'D3', ' ',
            'G3', ' ', 'A#3', ' ', 'C4', 'A#3', 'G3', 'F3',
            'F3', 'D3', 'C3', ' ', 'D3', ' ', ' ', ' '
        ];

        const bassCello = [
            'G2', ' ', ' ', ' ', 'C3', ' ', ' ', ' ',
            'F2', ' ', ' ', ' ', 'A#2', ' ', 'D2', ' ',
            'G2', ' ', ' ', ' ', 'C3', ' ', ' ', ' ',
            'F2', ' ', ' ', ' ', 'A#2', ' ', 'D2', ' ',
            'G2', 'G2', 'G2', 'G2', 'C3', 'C3', 'C3', 'C3',
            'F2', 'F2', 'F2', 'F2', 'A#2', 'A#2', 'D2', 'D2',
            'G2', 'G2', 'G2', 'G2', 'C3', 'C3', 'C3', 'C3',
            'F2', 'F2', 'F2', 'F2', 'G2', ' ', ' ', ' '
        ];

        const percussionHits = [
            1, 0, 0, 0, 1, 0, 1, 0, // Intro
            1, 0, 0, 0, 1, 0, 1, 0,
            1, 0, 1, 0, 1, 0, 1, 0, // Build-up
            1, 1, 1, 0, 1, 1, 1, 0,
            1, 1, 1, 1, 1, 1, 1, 1, // Climax
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 0, 1, 0, 1, 0, 1, 0, // Resolution
            1, 0, 0, 0, 1, 0, 0, 0
        ];


        // --- ابزارها و افکت‌ها ---
        const reverb = this.createReverbEffect();
        reverb.connect(this.audioContext.destination);

        const freqMap = {
            'G2': 98.00, 'A2': 110.00, 'A#2': 116.54, 'C3': 130.81, 'D3': 146.83, 'D#3': 155.56, 'F3': 174.61, 'G3': 196.00,
            'A3': 220.00, 'A#3': 233.08, 'C4': 261.63, 'D4': 293.66, 'D#4': 311.13, 'F4': 349.23, 'G4': 392.00,
            'F2': 87.31, 'D2': 73.42,
        };

        // --- توابع پخش‌کننده ---
        const playNote = (note, time, duration, config, targetNode) => {
            const freq = freqMap[note];
            if (!freq || note === ' ') return;

            const osc = this.audioContext.createOscillator();
            const gainNode = this.audioContext.createGain();

            osc.type = config.wave;
            osc.frequency.setValueAtTime(freq, time);
            if (config.detune) {
                osc.detune.setValueAtTime(config.detune, time); // برای صدای غنی‌تر
            }

            gainNode.gain.setValueAtTime(0, time);
            gainNode.gain.linearRampToValueAtTime(config.gain, time + (config.attack || 0.05));
            gainNode.gain.exponentialRampToValueAtTime(0.0001, time + duration);

            osc.connect(gainNode);
            gainNode.connect(targetNode);
            osc.start(time);
            osc.stop(time + duration);
        };

        const playPercussion = (time, gain, targetNode) => {
            const osc = this.audioContext.createOscillator();
            const gainNode = this.audioContext.createGain();
            osc.type = 'sine'; // موج سینوسی برای صدای بم طبل
            osc.frequency.setValueAtTime(80, time);
            osc.frequency.exponentialRampToValueAtTime(40, time + 0.5); // افت فرکانس برای حس ضربه

            gainNode.gain.setValueAtTime(gain, time);
            gainNode.gain.exponentialRampToValueAtTime(0.001, time + 0.5);

            osc.connect(gainNode);
            gainNode.connect(targetNode);
            osc.start(time);
            osc.stop(time + 0.5);
        };

        const loopMusic = () => {
            const startTime = this.audioContext.currentTime;
            const beatLength = 0.7;

            for (let i = 0; i < mainMelody.length; i++) {
                const time = startTime + i * beatLength;

                // ملودی اصلی (Synth Lead)
                // قبلاً
                // playNote(mainMelody[i], time, beatLength, { wave: 'sawtooth', gain: 0.05, detune: 5, attack: 0.01 }, reverb);
                playNote(mainMelody[i], time, beatLength, { wave: 'sawtooth', gain: 0.08, detune: 5, attack: 0.01 }, reverb);
                playNote(harmonyHorn[i], time, beatLength * 1.5, { wave: 'triangle', gain: 0.05, attack: 0.2 }, reverb);
                playNote(bassCello[i], time, beatLength * 1.8, { wave: 'sawtooth', gain: 0.06, attack: 0.1 }, reverb);


                // کوبه‌ای
                if (percussionHits[i] === 1) {
                    playPercussion(time, 0.25, reverb); // از 0.15 به 0.25
                }
            }

            const loopDuration = mainMelody.length * beatLength;
            setTimeout(loopMusic, loopDuration * 1000);
        };

        // شروع پخش
        loopMusic();
    }

    createReverbEffect() {
        const convolver = this.audioContext.createConvolver();
        const sampleRate = this.audioContext.sampleRate;
        const length = sampleRate * 2.5; // طنین ۲.۵ ثانیه‌ای
        const impulse = this.audioContext.createBuffer(2, length, sampleRate);
        const [impulseL, impulseR] = [impulse.getChannelData(0), impulse.getChannelData(1)];

        for (let i = 0; i < length; i++) {
            const val = (Math.random() * 2 - 1) * Math.pow(1 - i / length, 3);
            impulseL[i] = val;
            impulseR[i] = val;
        }
        convolver.buffer = impulse;
        return convolver;
    }

    playSpellSound(spellKey) {
        if (!this.audioContext) return;
        
        const spell = GAME_CONSTS.SPELLS[spellKey];
        const osc = this.audioContext.createOscillator();
        const gainNode = this.audioContext.createGain();
        
        // Different frequencies for different spells
        const freqMap = {
            'lumos': 880,
            'stupefy': 440,
            'expelliarmus': 660,
            'avada': 220,
            'patronum': 1320
        };
        
        osc.frequency.setValueAtTime(freqMap[spell.sound] || 440, this.audioContext.currentTime);
        osc.type = 'sine';
        
        gainNode.gain.setValueAtTime(0.1, this.audioContext.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.01, this.audioContext.currentTime + 0.3);
        
        osc.connect(gainNode);
        gainNode.connect(this.audioContext.destination);
        
        osc.start();
        osc.stop(this.audioContext.currentTime + 0.3);
    }

    resizeCanvas() {
        this.canvas.width = window.innerWidth;
        this.canvas.height = window.innerHeight;
    }

     initializePlayer() {
        return {
            x: 0,
            y: 0,
            radius: GAME_CONSTS.PLAYER_RADIUS,
            health: GAME_CONSTS.INITIAL_HEALTH,
            maxHealth: GAME_CONSTS.INITIAL_HEALTH,
            lives: GAME_CONSTS.INITIAL_LIVES,
            house: 'gryffindor',
            name: 'Harry Potter',
            direction: { x: 0, y: -1 },
            isStunned: false,
            stunTimer: 0,
            isMoving: false,
            currentZoneId: 'great_hall',
            patronusActive: false,
            patronusTimer: 0,
            isDead: false,
            
            // ✨ ویژگی‌های جدید انیمیشن
            animationState: 'idle', // 'idle', 'walking', 'casting'
            facing: 'right',      // 'left' یا 'right'
            frame: 0,             // فریم فعلی انیمیشن (ایندکس اسپریت شیت)
            animationTime: 0,     // زمان سپری شده برای فریم فعلی
            isCasting: false,     // کنترل فعال بودن انیمیشن طلسم
        };
    }

    async loadMap() {
        try {
            const response = await fetch('/hogwarts_map.json');
            this.mapData = await response.json();

            this.mapRenderer = new MapRenderer(this.canvas, this.mapData);
            await this.mapRenderer.loadImages();

            console.log('✅ Map and images loaded');

            this.resetGame();
            this.gameLoop(performance.now());

        } catch (error) {
            console.error('❌ Failed to load map:', error);
            this.renderErrorScreen();
        }
    }

    resetGame() {
        this.player = this.initializePlayer();
        
        const spawn = this.mapData.spawns.player;
        this.player.x = spawn.x * this.mapData.tileSize;
        this.player.y = spawn.y * this.mapData.tileSize;
        this.player.currentZoneId = 'great_hall';
        
        if (this.mapRenderer) {
            this.currentZone = this.mapRenderer.getZoneAt(this.player.x, this.player.y);
            this.mapRenderer.currentZone = this.currentZone;
            this.mapRenderer.centerCamera(this.player.x, this.player.y);
        }

        this.enemies = this.spawnEnemies();
        this.spells = [];
        this.particles = [];
        this.spellBeams = [];
        this.attachedDementors.clear();
        this.score = 0;
        this.keys = {};
        this.lastSpellTime = {};
        this.lastEnemyFire = new Map();
        this.message = 'Welcome to Hogwarts!';
        this.messageTimer = 3000;

        this.gameState = 'menu';
    }
    
    spawnEnemies() {
        const enemies = [];
        const ts = this.mapData.tileSize;

        // Spawn more Dementors (multiple per major room)
        const dementorSpawns = [
            { x: 85, y: 70, zone: 'courtyard' },
            { x: 95, y: 65, zone: 'courtyard' },
            { x: 85, y: 11, zone: 'library' },
            { x: 75, y: 15, zone: 'library' },
            { x: 95, y: 15, zone: 'library' },
            { x: 131, y: 70, zone: 'black_lake' },
            { x: 125, y: 75, zone: 'black_lake' },
            { x: 28, y: 86, zone: 'astronomy_tower' },
            { x: 35, y: 90, zone: 'astronomy_tower' },
            { x: 131, y: 93, zone: 'potions_classroom' },
        ];
        for (let i = 0; i < 8; i++) {
            const zone = this.mapData.zones.find(z => z.id === 'great_hall'); // یا هر منطقه دلخواه
            if (zone) {
              const spawnX = (zone.bounds.x + Math.random() * zone.bounds.width) * this.mapData.tileSize;
              const spawnY = (zone.bounds.y + Math.random() * zone.bounds.height) * this.mapData.tileSize;
              enemies.push({
                id: `slytherin_${i}`,
                x: spawnX,
                y: spawnY,
                radius: GAME_CONSTS.ENEMY_RADIUS,
                house: 'slytherin',
                name: 'Slytherin',
                health: 100,
                maxHealth: 100,
                type: 'enemy',
                isFalling: false,      // ✨ اضافه‌شده
                fallFrame: 0,          // ✨ کنترل انیمیشن افتادن
                isStunned: false,
                stunTimer: 0,
                currentZoneId: zone.id,
              });
            }
          }
        dementorSpawns.forEach((spawn, i) => {
            enemies.push({
                id: `dementor_${i}`,
                x: spawn.x * ts,
                y: spawn.y * ts,
                radius: GAME_CONSTS.ENEMY_RADIUS,
                house: 'dementor',
                name: 'Dementor',
                health: 150,
                maxHealth: 150,
                type: 'dementor',
                isStunned: false,
                stunTimer: 0,
                lastFireTime: 0,
                currentZoneId: spawn.zone,
                isFleeing: false,
                isPatronusTarget: true,
                attachedTo: null,
                targetDoor: null,
            });
        });

        // Regular enemies
        if (this.mapData.spawns.enemies) {
            this.mapData.spawns.enemies.forEach((spawn, i) => {
                enemies.push({
                    id: `enemy_${i}`,
                    x: spawn.x * ts,
                    y: spawn.y * ts,
                    radius: GAME_CONSTS.ENEMY_RADIUS,
                    house: spawn.house,
                    name: spawn.name,
                    health: 100,
                    maxHealth: 100,
                    type: 'enemy',
                    isStunned: false,
                    stunTimer: 0,
                    lastFireTime: 0,
                    currentZoneId: spawn.zone,
                    isFleeing: false,
                    targetDoor: null,
                });
            });
        }

        // Spawn Death Eaters randomly in various rooms
        const deathEaterCount = 3;
        const zones = ['great_hall', 'library', 'corridor', 'courtyard', 'gryffindor_common_room'];
        
        for (let i = 0; i < deathEaterCount; i++) {
            const zone = this.mapData.zones.find(z => z.id === zones[Math.floor(Math.random() * zones.length)]);
            if (zone) {
                const spawnX = (zone.bounds.x + zone.bounds.width / 2) * ts;
                const spawnY = (zone.bounds.y + zone.bounds.height / 2) * ts;
                
                enemies.push({
                    id: `deatheater_${i}`,
                    x: spawnX + (Math.random() - 0.5) * 200,
                    y: spawnY + (Math.random() - 0.5) * 200,
                    radius: GAME_CONSTS.ENEMY_RADIUS,
                    house: 'deatheater',
                    name: 'Death Eater',
                    health: 120,
                    maxHealth: 120,
                    type: 'deatheater',
                    isStunned: false,
                    stunTimer: 0,
                    lastFireTime: 0,
                    currentZoneId: zone.id,
                    isFleeing: false,
                    targetDoor: null,
                });
            }
        }

        return enemies;
    }

    setupEventListeners() {
        document.addEventListener('keydown', (e) => {
            const key = e.key.toUpperCase();
            this.keys[key] = true;
            
            if (this.gameState === 'menu' && e.code === 'Space') {
                e.preventDefault();
                this.startGame();
            }
            
            if (this.gameState === 'playing' && !e.repeat) {
                if (['Q', 'W', 'E', 'R', 'T'].includes(key)) {
                    e.preventDefault();
                    this.handleSpellCast(key);
                }
            }
            
            if (this.gameState === 'gameover' && e.code === 'Space') {
                e.preventDefault();
                this.resetGame();
                this.startGame();
            }
            
            if (this.gameState === 'victory' && e.code === 'Space') {
                e.preventDefault();
                this.resetGame();
                this.startGame();
            }
        });

        document.addEventListener('keyup', (e) => {
            const key = e.key.toUpperCase();
            this.keys[key] = false;
        });

        this.canvas.addEventListener('mousemove', (e) => {
            this.mousePos = { x: e.clientX, y: e.clientY };
        });
    }

    startGame() {
        this.gameState = 'playing';
        this.message = 'The duel has begun!';
        this.messageTimer = 2000;
        this.resizeCanvas();
    }

    update(deltaTime) {
        if (this.gameState !== 'playing') return;

        const dt = Math.min(deltaTime, 50);

        this.handlePlayerMovement(dt);
        this.updateAnimation(dt); // ✨ فراخوانی تابع انیمیشن

        this.updateSpells(dt);
        this.updateEnemies(dt);
        this.updateDementorAttachment(dt);
        this.checkCollisions();
        this.checkSpellCollisions();
        this.updateTimers(dt);
        this.updateSpellBeams(dt);

        if (this.messageTimer > 0) {
            this.messageTimer -= dt;
            if (this.messageTimer <= 0) {
                this.message = '';
            }
        }

        if (this.player.health <= 0) {
            this.player.lives--;
            if (this.player.lives > 0) {
                this.resetPlayerPosition();
            } else {
                this.gameState = 'gameover';
            }
        }

        if (this.enemies.length === 0) {
            this.gameState = 'victory';
        }
    }

    resetPlayerPosition() {
        const spawn = this.mapData.spawns.player;
        this.player.x = spawn.x * this.mapData.tileSize;
        this.player.y = spawn.y * this.mapData.tileSize;
        this.player.health = this.player.maxHealth;
        this.player.currentZoneId = 'great_hall';
        this.attachedDementors.clear();
        this.message = 'Life lost! Respawned.';
        this.messageTimer = 2000;
        
        if (this.mapRenderer) {
            this.mapRenderer.centerCamera(this.player.x, this.player.y);
        }
    }

    updateDementorAttachment(deltaTime) {
        this.attachedDementors.forEach((dementor, id) => {
            if (dementor.health <= 0) {
                this.attachedDementors.delete(id);
                return;
            }
            
            // Drain player health
            this.player.health -= (GAME_CONSTS.DEMENTOR_DRAIN_RATE * deltaTime) / 1000;
            
            // Keep dementor attached to player
            dementor.x = this.player.x;
            dementor.y = this.player.y;
            
            // If patronus is active, detach
            if (this.player.patronusActive) {
                dementor.attachedTo = null;
                this.attachedDementors.delete(id);
            }
        });
    }

    updateSpellBeams(deltaTime) {
        this.spellBeams = this.spellBeams.filter(beam => {
            beam.life -= deltaTime;
            beam.alpha = Math.max(0, beam.life / beam.maxLife);
            return beam.life > 0;
        });
    }

    checkSpellCollisions() {
        const spells = this.spells.filter(s => s.type !== 'patronus');
        
        for (let i = 0; i < spells.length; i++) {
            for (let j = i + 1; j < spells.length; j++) {
                const s1 = spells[i];
                const s2 = spells[j];
                
                // Skip if same source
                if (s1.source === s2.source) continue;
                
                const dist = Math.sqrt(
                    (s1.x - s2.x) * (s1.x - s2.x) + 
                    (s1.y - s2.y) * (s1.y - s2.y)
                );
                
                if (dist < (s1.radius + s2.radius) * 2) {
                    // Collision detected!
                    this.handleSpellCollision(s1, s2);
                }
            }
        }

          
    }

    handleSpellCollision(spell1, spell2) {
        // Create visual beam effect
        const blendColor = this.blendColors(spell1.color, spell2.color);
        
        this.spellBeams.push({
            x1: spell1.x,
            y1: spell1.y,
            x2: spell2.x,
            y2: spell2.y,
            color: blendColor,
            life: 2000,
            maxLife: 2000,
            alpha: 1.0
        });
        
        // Play collision sound
        if (this.audioContext) {
            const osc = this.audioContext.createOscillator();
            const gain = this.audioContext.createGain();
            osc.frequency.setValueAtTime(800, this.audioContext.currentTime);
            osc.type = 'square';
            gain.gain.setValueAtTime(0.05, this.audioContext.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.01, this.audioContext.currentTime + 0.2);
            osc.connect(gain);
            gain.connect(this.audioContext.destination);
            osc.start();
            osc.stop(this.audioContext.currentTime + 0.2);
        }
        
        // Determine winner based on power, then timestamp
        const power1 = spell1.power || 10;
        const power2 = spell2.power || 10;
        
        if (power1 > power2) {
            spell2.life = 0;
        } else if (power2 > power1) {
            spell1.life = 0;
        } else {
            // Equal power - first cast wins
            if (spell1.castTime < spell2.castTime) {
                spell2.life = 0;
            } else {
                spell1.life = 0;
            }
        }
    }

    blendColors(color1, color2) {
        const hex2rgb = (hex) => {
            const r = parseInt(hex.slice(1, 3), 16);
            const g = parseInt(hex.slice(3, 5), 16);
            const b = parseInt(hex.slice(5, 7), 16);
            return [r, g, b];
        };
        
        const rgb2hex = (r, g, b) => {
            return '#' + [r, g, b].map(x => {
                const hex = Math.round(x).toString(16);
                return hex.length === 1 ? '0' + hex : hex;
            }).join('');
        };
        
        const [r1, g1, b1] = hex2rgb(color1);
        const [r2, g2, b2] = hex2rgb(color2);
        
        return rgb2hex((r1 + r2) / 2, (g1 + g2) / 2, (b1 + b2) / 2);
    }

    updateTimers(deltaTime) {
        if (this.player.isStunned) {
            this.player.stunTimer -= deltaTime;
            if (this.player.stunTimer <= 0) {
                this.player.isStunned = false;
            }
        }

        if (this.player.patronusActive) {
            this.player.patronusTimer -= deltaTime;
            if (this.player.patronusTimer <= 0) {
                this.player.patronusActive = false;
                this.enemies.forEach(e => {
                    e.isFleeing = false;
                });
            } else {
                this.enemies.filter(e => e.type === 'dementor').forEach(d => {
                    d.isFleeing = true;
                });
            }
        }

        this.enemies.forEach(enemy => {
            if (enemy.isStunned) {
                enemy.stunTimer -= deltaTime;
                if (enemy.stunTimer <= 0) {
                    enemy.isStunned = false;
                }
            }
        });
    }

    handlePlayerMovement(deltaTime) {
        // اگر پلیر در حال طلسم زدن باشد، نباید بتواند حرکت کند و انیمیشن طلسم باید اولویت داشته باشد.
        if (this.player.isStunned || this.player.isCasting) return;
    
        let vx = 0;
        let vy = 0;
    
        if (this.keys['ARROWUP']) vy -= 1;
        if (this.keys['ARROWDOWN']) vy += 1;
        if (this.keys['ARROWLEFT']) vx -= 1;
        if (this.keys['ARROWRIGHT']) vx += 1;
    
        // ✨ منطق انیمیشن: تعیین وضعیت حرکت (isMoving)
        if (vx !== 0 || vy !== 0) {
            
            // ✨ منطق انیمیشن: تعیین جهت نگاه (facing)
            // جهت افقی بر جهت عمودی اولویت دارد (یا آخرین جهت ثبت شده حفظ می شود)
            if (vx > 0) this.player.facing = 'right';
            else if (vx < 0) this.player.facing = 'left';
            else if (vy < 0) this.player.facing = 'up';
            else if (vy > 0) this.player.facing = 'down';
            
            const magnitude = Math.sqrt(vx * vx + vy * vy);
            const speed = GAME_CONSTS.PLAYER_SPEED;

            const normalizedVx = vx / magnitude;
            const normalizedVy = vy / magnitude;
            
            const moveX = normalizedVx * speed;
            const moveY = normalizedVy * speed;
            
            let newX = this.player.x + moveX;
            let newY = this.player.y + moveY;
            
            this.player.direction.x = normalizedVx;
            this.player.direction.y = normalizedVy;
            this.player.isMoving = true; // ✨ پلیر در حال حرکت است
            
            const doorInfo = this.mapRenderer.checkDoorTransition(newX, newY, this.player.radius);
            if (doorInfo) {
                // اگر در حال عبور از درب باشد
                this.player.x = newX;
                this.player.y = newY;
                this.handleZoneTransition(doorInfo);
                return;
            }
            
            // ... (منطق بررسی برخورد با دیواره‌ها)
            const canMoveX = !this.mapRenderer.checkCollision(newX, this.player.y, this.player.radius);
            const canMoveY = !this.mapRenderer.checkCollision(this.player.x, newY, this.player.radius);
            const canMoveBoth = !this.mapRenderer.checkCollision(newX, newY, this.player.radius);
            
            if (canMoveBoth) {
                this.player.x = newX;
                this.player.y = newY;
            } else if (canMoveX) {
                this.player.x = newX;
                this.player.direction.x = normalizedVx;
                this.player.direction.y = 0;
            } else if (canMoveY) {
                this.player.y = newY;
                this.player.direction.x = 0;
                this.player.direction.y = normalizedVy;
            }
            
        } else {
            this.player.isMoving = false; // ✨ پلیر ایستاده (idle) است
        }
    }

    handleZoneTransition(doorInfo) {
        const { currentSide, targetZoneId, currentZone } = doorInfo;
        const targetZone = this.mapData.zones.find(z => z.id === targetZoneId);
      
        if (!targetZone) {
            console.error(`❌ Target zone ${targetZoneId} not found!`);
            return;
        }
      
        const currentZoneId = currentZone ? currentZone.id : this.player.currentZoneId;
        const targetExit = targetZone.exits?.find(exit => exit.connects_to === currentZoneId);
        
        if (!targetExit) {
            console.warn(`⚠️ No matching exit in ${targetZoneId} for ${currentZoneId}`);
            return;
        }
      
        const ts = this.mapData.tileSize;
        const { x: tx, y: ty, width: tw, height: th } = targetZone.bounds;
        const doorSize = 3;
        const doorCenter = (targetExit.door_at ?? Math.floor(
            (targetExit.side === 'north' || targetExit.side === 'south' ? tw : th) / 2 - doorSize / 2
        )) + doorSize / 2;
      
        const offset = ts * 1.5;
      
        let spawnX = 0;
        let spawnY = 0;
      
        switch (targetExit.side) {
            case 'north':
                spawnX = (tx + doorCenter) * ts;
                spawnY = (ty + 1.5) * ts + offset;
                break;
            case 'south':
                spawnX = (tx + doorCenter) * ts;
                spawnY = (ty + th - 1.5) * ts - offset;
                break;
            case 'west':
                spawnX = (tx + 1.5) * ts + offset;
                spawnY = (ty + doorCenter) * ts;
                break;
            case 'east':
                spawnX = (tx + tw - 1.5) * ts - offset;
                spawnY = (ty + doorCenter) * ts;
                break;
        }
      
        this.player.x = spawnX;
        this.player.y = spawnY;
        this.player.currentZoneId = targetZoneId;
        this.currentZone = targetZone;
        this.message = `Entered ${targetZone.name}!`;
        this.messageTimer = 2000;
      
        console.log(`✅ Teleported to ${targetZone.id} (${Math.round(spawnX)}, ${Math.round(spawnY)})`);
      
        if (this.mapRenderer) {
            this.mapRenderer.currentZone = targetZone;
            this.mapRenderer.centerCamera(this.player.x, this.player.y);
        }
      
        this.keys = {};
    }

  // index.js (متد handleSpellCast اصلاح شده)

    handleSpellCast(key) {
        const wandOffset = {
            right: { x: 40, y: -15 },
            left: { x: -40, y: -15 },
            up: { x: 0, y: -50 },
            down: { x: 0, y: 50 }
          };
          const offset = wandOffset[this.player.facing] || { x: 0, y: 0 };
          
          const sourceX = this.player.x + offset.x;
          const sourceY = this.player.y + offset.y;


        const spellDef = GAME_CONSTS.SPELLS[key];
        if (!spellDef) return;
        
        const now = performance.now();

        if (this.lastSpellTime[key] && now - this.lastSpellTime[key] < spellDef.cooldown) {
            const remaining = Math.ceil((spellDef.cooldown - (now - this.lastSpellTime[key])) / 1000);
            this.message = `${spellDef.name} cooldown: ${remaining}s`;
            this.messageTimer = 800;
            return;
        }

        // ✨ ۱. فعال کردن انیمیشن طلسم
        this.player.isCasting = true;
        this.player.frame = 0;
        this.player.animationTime = 0;
        
        // بقیه کدهای منطق پرتاب طلسم از اینجا شروع می‌شوند
        this.lastSpellTime[key] = now;
        this.message = `✨ ${spellDef.name}!`;
        this.messageTimer = 1500;
        
        // Play spell sound
        this.playSpellSound(key);

        if (spellDef.type === 'patronus') {
            this.player.patronusActive = true;
            this.player.patronusTimer = 5000;
            
            // Detach all dementors
            this.attachedDementors.clear();
            this.enemies.forEach(e => {
                if (e.type === 'dementor') {
                    e.attachedTo = null;
                }
            });
            
            this.spells.push({
                x: sourceX,
                y: sourceY,
                sourceX: sourceX,
                sourceY: sourceY,
                vx: vx,
                vy: vy,
                radius: GAME_CONSTS.SPELL_RADIUS,
                color: spellDef.color,
                damage: spellDef.damage,
                type: spellDef.type,
                source: 'player',
                life: GAME_CONSTS.SPELL_RANGE,
                range: GAME_CONSTS.SPELL_RANGE,
                zoneId: this.player.currentZoneId,
                power: spellDef.power,
                castTime: now,
            });
            return;
        }

        let dx = this.player.direction.x;
        let dy = this.player.direction.y;
        
        if (dx === 0 && dy === 0) {
            // اگر پلیر حرکت نمی‌کند، باید طلسم در جهتی که نگاه می‌کند پرتاب شود.
            // چون منطق `handlePlayerMovement` جهت نگاه (facing) را تنظیم می‌کند،
            // اگر جهت (direction) صفر است، ما فرض می‌کنیم که پلیر به سمت جلو (جهت نگاه) پرتاب می‌کند.
            
            // در اینجا می‌توانیم جهت را بر اساس facing پلیر تعیین کنیم:
            if (this.player.facing === 'right') {
                dx = 1;
                dy = 0; // فقط افقی
            } else {
                dx = -1;
                dy = 0; // فقط افقی
            }
        }
        
        const magnitude = Math.sqrt(dx * dx + dy * dy);
        const vx = (dx / magnitude) * spellDef.speed;
        const vy = (dy / magnitude) * spellDef.speed;

        this.spells.push({
            x: this.player.x,
            y: this.player.y,
            vx: vx,
            vy: vy,
            radius: GAME_CONSTS.SPELL_RADIUS,
            color: spellDef.color,
            damage: spellDef.damage,
            type: spellDef.type,
            source: 'player',
            life: GAME_CONSTS.SPELL_RANGE,
            range: GAME_CONSTS.SPELL_RANGE,
            zoneId: this.player.currentZoneId,
            power: spellDef.power,
            castTime: now,
        });
        setTimeout(() => {
            this.player.isCasting = false;
          }, 400);
    }
    updateAnimation(deltaTime) {
        const player = this.player;
        if (!this.mapRenderer || !this.mapRenderer.animationData) return;
      
        const animData = this.mapRenderer.animationData;
        let animKey;
      
        // انتخاب انیمیشن بر اساس وضعیت
        if (player.isCasting) {
          // برای طلسم فقط همون حالت ایستاده در همون جهت رو نشون بده
          animKey = player.facing === 'right' ? 'magic_right' :
                    player.facing === 'left'  ? 'magic_left'  :
                    player.facing === 'up'    ? 'stand_up'    :
                    player.facing === 'down'  ? 'stand_down'  : null;
          player.animationState = 'casting';
        } 
        else if (player.isMoving) {
          animKey = 'walk_' + player.facing;
          player.animationState = 'walking';
        } 
        else {
          player.animationState = 'idle';
          player.frame = 0;
          player.animationTime = 0;
          return;
        }
      
        const currentAnim = animData[animKey];
        if (!currentAnim) return;
      
        // محاسبه فریم
        player.animationTime += deltaTime;
        const frameDuration = 1000 / currentAnim.fps;
      
        if (player.animationTime >= frameDuration) {
          const framesPassed = Math.floor(player.animationTime / frameDuration);
          player.frame = (player.frame + framesPassed) % currentAnim.frames;
          player.animationTime %= frameDuration;
      
          // ✅ پایان انیمیشن طلسم
          if (player.animationState === 'casting' && player.frame === currentAnim.frames - 1) {
            // بعد از رسیدن به آخرین فریم، طلسم تموم شده
            player.isCasting = false;
            player.frame = 0;
          }
        }
      }
      
    updateSpells(deltaTime) {
        this.spells = this.spells.filter(spell => {
            if (spell.type === 'patronus') {
                spell.life -= deltaTime;
                
                // Patronus seeks nearest dementor
                const nearestDementor = this.findNearestDementor(spell.x, spell.y);
                if (nearestDementor) {
                    const dx = nearestDementor.x - spell.x;
                    const dy = nearestDementor.y - spell.y;
                    const dist = Math.sqrt(dx * dx + dy * dy);
                    
                    if (dist > 10) {
                        spell.vx = (dx / dist) * GAME_CONSTS.PATRONUS_SEEK_SPEED;
                        spell.vy = (dy / dist) * GAME_CONSTS.PATRONUS_SEEK_SPEED;
                        spell.x += spell.vx;
                        spell.y += spell.vy;
                    }
                }
                
                return spell.life > 0;
            }

            spell.x += spell.vx;
            spell.y += spell.vy;
            
            const travelDistance = Math.sqrt(spell.vx * spell.vx + spell.vy * spell.vy);
            spell.life -= travelDistance;
    
            if (this.mapRenderer && this.mapRenderer.checkCollision(spell.x, spell.y, spell.radius)) {
                return false;
            }
    
            return spell.life > 0;
        });
    }

    findNearestDementor(x, y) {
        let nearest = null;
        let minDist = Infinity;
        
        this.enemies.forEach(e => {
            if (e.type === 'dementor' && e.health > 0) {
                const dist = Math.sqrt((e.x - x) ** 2 + (e.y - y) ** 2);
                if (dist < minDist) {
                    minDist = dist;
                    nearest = e;
                }
            }
        });
        
        return nearest;
    }

    updateEnemies(deltaTime) {
        const now = performance.now();
        
        this.enemies.forEach(enemy => {
            if (enemy.health <= 0) return;
            if (enemy.isStunned) return;
            if (enemy.attachedTo === 'player') return;

            // Check if player is in same zone
            const sameZone = enemy.currentZoneId === this.player.currentZoneId;
            
            // Check if player is visible through a door
            const playerVisible = sameZone || this.canSeePlayerThroughDoor(enemy);

            if (!playerVisible) {
                enemy.targetDoor = null;
                return;
            }

            // Dementor specific behavior
            if (enemy.type === 'dementor') {
                this.updateDementor(enemy, deltaTime, sameZone);
                return;
            }

            // Regular enemy behavior
            let targetX = this.player.x;
            let targetY = this.player.y;
            let speed = GAME_CONSTS.ENEMY_CHASE_SPEED;

            if (enemy.isFleeing && this.player.patronusActive) {
                targetX = enemy.x + (enemy.x - this.player.x);
                targetY = enemy.y + (enemy.y - this.player.y);
                speed = GAME_CONSTS.ENEMY_CHASE_SPEED * 1.5;
            }

            // If in different zone, navigate to door
            if (!sameZone && enemy.targetDoor) {
                targetX = enemy.targetDoor.x;
                targetY = enemy.targetDoor.y;
            }

            const dx = targetX - enemy.x;
            const dy = targetY - enemy.y;
            const distance = Math.sqrt(dx * dx + dy * dy);

            if (distance > enemy.radius + this.player.radius + 20) {
                const magnitude = Math.sqrt(dx * dx + dy * dy);
                const vx = (dx / magnitude) * speed;
                const vy = (dy / magnitude) * speed;

                const newX = enemy.x + vx;
                const newY = enemy.y + vy;

                enemy.direction = { x: vx, y: vy };

                if (!this.mapRenderer.checkCollision(newX, newY, enemy.radius)) {
                    enemy.x = newX;
                    enemy.y = newY;
                    
                    // Update zone if passed through door
                    const newZone = this.mapRenderer.getZoneAt(enemy.x, enemy.y);
                    if (newZone && newZone.id !== enemy.currentZoneId) {
                        enemy.currentZoneId = newZone.id;
                        enemy.targetDoor = null;
                    }
                }

                // اگر دشمن به هدف در نزدیک شد، وارد منطقه جدید شود
                if (enemy.targetDoor) {
                    const distToDoor = Math.sqrt(
                        (enemy.x - enemy.targetDoor.x) ** 2 +
                        (enemy.y - enemy.targetDoor.y) ** 2
                    );

                    if (distToDoor < GAME_CONSTS.DOOR_DETECTION_RANGE / 3) {
                        // پیدا کردن zone مقصد از روی خروجی فعلی
                        const currentZone = this.mapData.zones.find(z => z.id === enemy.currentZoneId);
                        if (currentZone) {
                            const matchingExit = currentZone.exits?.find(
                                exit => exit.connects_to === this.player.currentZoneId
                            );
                            if (matchingExit) {
                                enemy.currentZoneId = matchingExit.connects_to;
                                enemy.targetDoor = null;
                                console.log(`🚪 ${enemy.name} passed door to ${enemy.currentZoneId}`);
                            }
                        }
                    }
                }

            }

            // Death Eater only shoots Avada Kedavra
            if (enemy.type === 'deatheater' && sameZone) {
                if (distance < 500 && now - enemy.lastFireTime > GAME_CONSTS.ENEMY_FIRE_RATE) {
                    this.handleEnemyFire(enemy, dx, dy, distance, true);
                    enemy.lastFireTime = now;
                }
            } else if (sameZone && distance < 400 && now - enemy.lastFireTime > GAME_CONSTS.ENEMY_FIRE_RATE) {
                this.handleEnemyFire(enemy, dx, dy, distance, false);
                enemy.lastFireTime = now;
            }
        });
        
        this.enemies = this.enemies.filter(e => e.health > 0);
    }

    updateDementor(dementor, deltaTime, sameZone) {
        if (dementor.isFleeing && this.player.patronusActive) {
            const dx = dementor.x - this.player.x;
            const dy = dementor.y - this.player.y;
            const distance = Math.sqrt(dx * dx + dy * dy);
            
            if (distance < 300) {
                const vx = (dx / distance) * GAME_CONSTS.DEMENTOR_CHASE_SPEED * 2;
                const vy = (dy / distance) * GAME_CONSTS.DEMENTOR_CHASE_SPEED * 2;
                
                const newX = dementor.x + vx;
                const newY = dementor.y + vy;
                
                if (!this.mapRenderer.checkCollision(newX, newY, dementor.radius)) {
                    dementor.x = newX;
                    dementor.y = newY;
                }
            }
            return;
        }

        let targetX = this.player.x;
        let targetY = this.player.y;

        // Navigate to door if in different zone
        if (!sameZone && dementor.targetDoor) {
            targetX = dementor.targetDoor.x;
            targetY = dementor.targetDoor.y;
        }

        const dx = targetX - dementor.x;
        const dy = targetY - dementor.y;
        const distance = Math.sqrt(dx * dx + dy * dy);

        // Check if close enough to attach
        if (sameZone && distance < GAME_CONSTS.DEMENTOR_ATTACH_DISTANCE) {
            dementor.attachedTo = 'player';
            this.attachedDementors.set(dementor.id, dementor);
            return;
        }

        // Chase player
        if (distance > 10) {
            const vx = (dx / distance) * GAME_CONSTS.DEMENTOR_CHASE_SPEED;
            const vy = (dy / distance) * GAME_CONSTS.DEMENTOR_CHASE_SPEED;

            const newX = dementor.x + vx;
            const newY = dementor.y + vy;

            if (!this.mapRenderer.checkCollision(newX, newY, dementor.radius)) {
                dementor.x = newX;
                dementor.y = newY;
                
                // Update zone if passed through door
                const newZone = this.mapRenderer.getZoneAt(dementor.x, dementor.y);
                if (newZone && newZone.id !== dementor.currentZoneId) {
                    dementor.currentZoneId = newZone.id;
                    dementor.targetDoor = null;
                }
            }
        }
    }

    canSeePlayerThroughDoor(enemy) {
        const enemyZone = this.mapData.zones.find(z => z.id === enemy.currentZoneId);
        if (!enemyZone || !enemyZone.exits) return false;

        for (const exit of enemyZone.exits) {
            if (exit.connects_to === this.player.currentZoneId) {
                // Calculate door position
                const doorPos = this.calculateDoorPosition(enemyZone, exit);
                const distToDoor = Math.sqrt(
                    (doorPos.x - enemy.x) ** 2 + 
                    (doorPos.y - enemy.y) ** 2
                );

                if (distToDoor < GAME_CONSTS.ENEMY_VISION_RANGE) {
                    enemy.targetDoor = doorPos;
                    return true;
                }
            }
        }

        return false;
    }

    calculateDoorPosition(zone, exit) {
        const ts = this.mapData.tileSize;
        const { x, y, width, height } = zone.bounds;
        const doorSize = 3;
        const doorStart = exit.door_at ?? Math.floor(
            (exit.side === 'north' || exit.side === 'south' ? width : height) / 2 - doorSize / 2
        );
        const doorCenter = doorStart + doorSize / 2;

        let doorX = 0, doorY = 0;

        switch (exit.side) {
            case 'north':
                doorX = (x + doorCenter) * ts;
                doorY = y * ts;
                break;
            case 'south':
                doorX = (x + doorCenter) * ts;
                doorY = (y + height) * ts;
                break;
            case 'west':
                doorX = x * ts;
                doorY = (y + doorCenter) * ts;
                break;
            case 'east':
                doorX = (x + width) * ts;
                doorY = (y + doorCenter) * ts;
                break;
        }

        return { x: doorX, y: doorY };
    }

    handleEnemyFire(enemy, dx, dy, distance, isAvadaKedavra = false) {
        const vx = (dx / distance) * 5 * 0.7;
        const vy = (dy / distance) * 5 * 0.7;

        let color = '#FFD700';
        let type = 'enemy_spell';
        let damage = GAME_CONSTS.ENEMY_DAMAGE;
        let power = 15;
        
        if (isAvadaKedavra) {
            color = '#00ff00';
            type = 'death';
            damage = 100;
            power = 100;
        } else if (enemy.house === 'deatheater') {
            color = '#800080';
        } else if (enemy.house === 'dementor') {
            color = '#333333';
        }

        this.spells.push({
            x: enemy.x,
            y: enemy.y,
            vx: vx,
            vy: vy,
            radius: GAME_CONSTS.SPELL_RADIUS * 0.8,
            color: color,
            damage: damage,
            type: type,
            source: 'enemy',
            life: 350,
            range: 350,
            zoneId: enemy.currentZoneId,
            power: power,
            castTime: performance.now(),
        });
    }

    checkCollisions() {
        this.spells.forEach(spell => {
            if (spell.source === 'player') {
                this.enemies.forEach(enemy => {
                    if (enemy.currentZoneId !== this.player.currentZoneId) return;
                    
                    const dist = Math.sqrt(
                        (spell.x - enemy.x) * (spell.x - enemy.x) + 
                        (spell.y - enemy.y) * (spell.y - enemy.y)
                    );
                    if (dist < spell.radius + enemy.radius) {
                        this.applySpellEffect(spell, enemy);
                        if (spell.type !== 'patronus') {
                            spell.life = 0;
                        }
                    }
                });
            } else if (spell.source === 'enemy') {
                const dist = Math.sqrt(
                    (spell.x - this.player.x) * (spell.x - this.player.x) + 
                    (spell.y - this.player.y) * (spell.y - this.player.y)
                );
                if (dist < spell.radius + this.player.radius) {
                    this.applySpellEffect(spell, this.player);
                    spell.life = 0;
                }
            }
        });

                // بعد از کم کردن سلامتی دشمن:
        this.spells.forEach(spell => {
            if (spell.source === 'player') {
                this.enemies.forEach(enemy => {
                if (enemy.currentZoneId !== this.player.currentZoneId) return;
            
                const dist = Math.sqrt(
                    (spell.x - enemy.x) * (spell.x - enemy.x) +
                    (spell.y - enemy.y) * (spell.y - enemy.y)
                );
                if (dist < spell.radius + enemy.radius) {
                    this.applySpellEffect(spell, enemy);
                    if (spell.type !== 'patronus') {
                    spell.life = 0;
                    }
            
                    // ✅ درست‌ترین محل برای کد افتادن:
                // انیمیشن افتادن Slytherin
                if (enemy.house === 'slytherin' && !enemy.isFalling) {
                    if (spell.type === 'disarm' || enemy.health <= 0) {
                        enemy.isFalling = true;
                        enemy.fallFrame = 0;
                        
                        // جهت پرت شدن از spell
                        enemy.fallDirX = spell.vx / Math.abs(spell.vx || 1);
                        enemy.fallDirY = spell.vy / Math.abs(spell.vy || 1);
                        enemy.fallFlip = spell.vx < 0;
                                }
                            }
                        }
                    });
                      
                }
                });

                  
  
        if (this.player.patronusActive) {
            const patronusSpell = this.spells.find(s => s.type === 'patronus');
            if (patronusSpell) {
                this.enemies.filter(e => e.type === 'dementor' && e.currentZoneId === this.player.currentZoneId).forEach(dementor => {
                    const dist = Math.sqrt(
                        (dementor.x - patronusSpell.x) * (dementor.x - patronusSpell.x) + 
                        (dementor.y - patronusSpell.y) * (dementor.y - patronusSpell.y)
                    );
                    if (dist < patronusSpell.radius + dementor.radius * 2) {
                        dementor.health -= GAME_CONSTS.PATRONUS_DAMAGE_RATE;
                        dementor.isFleeing = true;
                        
                        // Detach if attached
                        if (dementor.attachedTo === 'player') {
                            dementor.attachedTo = null;
                            this.attachedDementors.delete(dementor.id);
                        }
                    } else if (dist < 400) {
                        dementor.isFleeing = true;
                    }
                });
            }
        }

        this.spells = this.spells.filter(s => s.life > 0);
    }

    applySpellEffect(spell, target) {

        // 💥 افکت نور هنگام برخورد
        this.spellBeams.push({
            x1: spell.x - spell.vx * 3,
            y1: spell.y - spell.vy * 3,
            x2: spell.x + spell.vx * 3,
            y2: spell.y + spell.vy * 3,
            color: spell.color,
            life: 300,
            maxLife: 300,
            alpha: 1.0
        });
        

        if (spell.type === 'patronus') {
            if (target.type === 'dementor') {
                target.health -= 0.3;
            }
            return;
        }

        target.health -= spell.damage;

        if (spell.type === 'stun') {
            target.isStunned = true;
            target.stunTimer = 1500;
        }

        if (spell.type === 'death' && target !== this.player) {
            target.health = 0;
        }

        if (target.health <= 0) {
            if (target !== this.player) {
                this.score += target.maxHealth;
                this.message = `${target.name} Defeated! (+${target.maxHealth} pts)`;
                this.messageTimer = 2000;
            }
        }
        if (target.house === 'slytherin' && !target.isFalling) {
        if (spell.type === 'disarm' || target.health <= 0) {
            target.isFalling = true;
            target.fallFrame = 0;

            // ✨ تشخیص جهت پرتاب (بر اساس موقعیت بازیکن)
            const dx = target.x - this.player.x;
            target.fallDirX = dx >= 0 ? 1 : -1; // اگر دشمن سمت راست بازیکنه → پرت شدن به راست
            target.fallDirY = 0; // فعلاً فقط افقی
            target.fallFlip = dx < 0; // اگر از سمت چپ بوده → flip شود
        }
        }
    }

    render() {
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    
        if (this.gameState === 'menu') {
            this.renderMenu();
            return;
        }
    
        if (!this.mapRenderer || !this.mapRenderer.imagesLoaded) {
            this.ctx.fillStyle = '#000';
            this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
            this.ctx.fillStyle = '#FFD700';
            this.ctx.font = '24px Arial';
            this.ctx.textAlign = 'center';
            this.ctx.fillText('Loading Hogwarts...', this.canvas.width / 2, this.canvas.height / 2);
            return;
        }
    
        const visibleEnemies = this.enemies.filter(e => 
            e.currentZoneId === this.player.currentZoneId
        );
        
        const visibleSpells = this.spells.filter(s =>
            s.zoneId === this.player.currentZoneId || s.type === 'patronus'
        );
        
        const allEntities = [
            { ...this.player, type: 'player' },
            ...visibleEnemies,
            ...visibleSpells.map(s => ({ ...s, type: 'spell' }))
        ];
    
        this.mapRenderer.render(this.player.x, this.player.y, allEntities, this.player);
        this.mapRenderer.update();
        
        // Render spell beams on top
        this.renderSpellBeams();
        
        this.renderUI();
        
        if (this.gameState === 'gameover') {
            this.renderEndScreen('💀 GAME OVER 💀', 'You were defeated...', '#FF0000');
        }
        
        if (this.gameState === 'victory') {
            this.renderEndScreen('🎉 VICTORY 🎉', 'All enemies defeated!', '#00FF00');
        }
    }

    renderSpellBeams() {
        this.ctx.save();
        
        this.spellBeams.forEach(beam => {
            // Convert world coordinates to screen coordinates
            const screen1 = this.mapRenderer.worldToScreen(beam.x1, beam.y1);
            const screen2 = this.mapRenderer.worldToScreen(beam.x2, beam.y2);
            
            this.ctx.globalAlpha = beam.alpha;
            this.ctx.strokeStyle = beam.color;
            this.ctx.lineWidth = 8;
            this.ctx.shadowBlur = 20;
            this.ctx.shadowColor = beam.color;
            
            this.ctx.beginPath();
            this.ctx.moveTo(screen1.x, screen1.y);
            this.ctx.lineTo(screen2.x, screen2.y);
            this.ctx.stroke();
            
            // Add glow effect
            this.ctx.lineWidth = 3;
            this.ctx.strokeStyle = '#ffffff';
            this.ctx.beginPath();
            this.ctx.moveTo(screen1.x, screen1.y);
            this.ctx.lineTo(screen2.x, screen2.y);
            this.ctx.stroke();
        });
        
        this.ctx.restore();
    }
    
    renderMenu() {
        this.ctx.fillStyle = 'rgba(0, 0, 0, 0.95)';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

        this.ctx.fillStyle = '#FFD700';
        this.ctx.font = 'bold 60px Georgia, serif';
        this.ctx.textAlign = 'center';
        this.ctx.fillText('Hogwarts: Combat Arena', this.canvas.width / 2, this.canvas.height / 2 - 100);

        this.ctx.fillStyle = '#FFF';
        this.ctx.font = '24px Georgia, serif';
        this.ctx.fillText('Press SPACE to Start Combat', this.canvas.width / 2, this.canvas.height / 2 + 20);
        
        this.ctx.font = '18px Georgia, serif';
        this.ctx.fillText('Arrow Keys: Move', this.canvas.width / 2, this.canvas.height / 2 + 70);
        
        this.ctx.font = '16px Georgia, serif';
        this.ctx.fillStyle = '#ffffff';
        this.ctx.fillText('Q: Lumos', this.canvas.width / 2 - 200, this.canvas.height / 2 + 110);
        this.ctx.fillStyle = '#ff0000';
        this.ctx.fillText('W: Stupefy', this.canvas.width / 2 - 60, this.canvas.height / 2 + 110);
        this.ctx.fillStyle = '#ff6600';
        this.ctx.fillText('E: Expelliarmus', this.canvas.width / 2 + 90, this.canvas.height / 2 + 110);
        
        this.ctx.fillStyle = '#00ff00';
        this.ctx.fillText('R: Avada Kedavra', this.canvas.width / 2 - 80, this.canvas.height / 2 + 140);
        this.ctx.fillStyle = '#99ccff';
        this.ctx.fillText('T: Expecto Patronum', this.canvas.width / 2 + 100, this.canvas.height / 2 + 140);
    }

    renderUI() {
        const padding = 20;

        this.ctx.fillStyle = 'rgba(0, 0, 0, 0.75)';
        this.ctx.fillRect(padding, padding, 280, 200);

        this.ctx.fillStyle = '#FF4500';
        this.ctx.font = 'bold 20px Arial';
        this.ctx.textAlign = 'left';
        this.ctx.fillText(`Health: ${Math.round(this.player.health)}/${this.player.maxHealth}`, padding + 15, padding + 30);
        
        this.ctx.fillStyle = '#FFD700';
        this.ctx.fillText(`Score: ${this.score}`, padding + 15, padding + 55);

        this.ctx.fillStyle = '#3CB371';
        this.ctx.fillText(`Lives: ${this.player.lives}`, padding + 15, padding + 80);

        this.ctx.font = 'bold 14px Arial';
        const now = performance.now();
        let yOffset = 110;
        
        Object.entries(GAME_CONSTS.SPELLS).forEach(([key, spell]) => {
            const lastCast = this.lastSpellTime[key] || 0;
            const timeSince = now - lastCast;
            const isReady = timeSince >= spell.cooldown;
            
            this.ctx.fillStyle = isReady ? spell.color : '#666';
            const cooldownText = isReady ? '✓' : `${Math.ceil((spell.cooldown - timeSince) / 1000)}s`;
            this.ctx.fillText(`${key}: ${spell.name} ${cooldownText}`, padding + 15, padding + yOffset);
            yOffset += 18;
        });

        if (this.player.patronusActive) {
            this.ctx.fillStyle = '#99ccff';
            this.ctx.font = 'bold 16px Arial';
            this.ctx.fillText('🛡️ PATRONUS ACTIVE', padding + 15, padding + yOffset + 5);
        }
        
        if (this.attachedDementors.size > 0) {
            this.ctx.fillStyle = '#ff0000';
            this.ctx.font = 'bold 16px Arial';
            this.ctx.fillText(`⚠️ ${this.attachedDementors.size} DEMENTOR(S) ATTACHED!`, padding + 15, padding + yOffset + 25);
        }

        if (this.message && this.messageTimer > 0) {
            this.ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
            this.ctx.fillRect(this.canvas.width / 2 - 200, this.canvas.height - 100, 400, 50);

            this.ctx.fillStyle = '#FFF';
            this.ctx.font = 'bold 18px Arial';
            this.ctx.textAlign = 'center';
            this.ctx.fillText(this.message, this.canvas.width / 2, this.canvas.height - 70);
        }
    }
    
    renderEndScreen(title, subtitle, color) {
        this.ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

        this.ctx.fillStyle = color;
        this.ctx.font = 'bold 72px Georgia, serif';
        this.ctx.textAlign = 'center';
        this.ctx.fillText(title, this.canvas.width / 2, this.canvas.height / 2 - 50);

        this.ctx.fillStyle = '#FFF';
        this.ctx.font = '24px Georgia, serif';
        this.ctx.fillText(subtitle, this.canvas.width / 2, this.canvas.height / 2 + 10);
        
        this.ctx.font = '20px Georgia, serif';
        this.ctx.fillText(`Final Score: ${this.score}`, this.canvas.width / 2, this.canvas.height / 2 + 50);

        this.ctx.fillStyle = '#FFD700';
        this.ctx.font = '24px Georgia, serif';
        this.ctx.fillText('Press SPACE to Restart', this.canvas.width / 2, this.canvas.height / 2 + 120);
    }

    renderErrorScreen() {
        this.ctx.fillStyle = '#000';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        
        this.ctx.fillStyle = '#FF0000';
        this.ctx.font = 'bold 32px Arial';
        this.ctx.textAlign = 'center';
        this.ctx.fillText('Error Loading Map', this.canvas.width / 2, this.canvas.height / 2);
        
        this.ctx.fillStyle = '#FFF';
        this.ctx.font = '18px Arial';
        this.ctx.fillText('Please check console for details', this.canvas.width / 2, this.canvas.height / 2 + 40);
    }

    gameLoop(timestamp) {
        const deltaTime = timestamp - this.lastUpdateTime;
        this.lastUpdateTime = timestamp;

        this.update(deltaTime);
        this.render();

        this.animationFrameId = requestAnimationFrame((ts) => this.gameLoop(ts));
    }
}

window.addEventListener('DOMContentLoaded', () => {
    const canvas = document.getElementById('gameCanvas');
    if (!canvas) {
        console.error("Canvas element with ID 'gameCanvas' not found!");
        return;
    }
    
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
    
    const game = new HogwartsGame(canvas);
    window.hogwartsGame = game;
    
    console.log('🎮 Hogwarts Combat Arena Started!');
    console.log('🪄 Spells: Q=Lumos, W=Stupefy, E=Expelliarmus, R=Avada Kedavra, T=Patronus');
    console.log('⬆️⬇️⬅️➡️ Arrow Keys to Move');
    console.log('✨ NEW: Enemies follow through doors, Patronus seeks Dementors, Spell collisions!');
});