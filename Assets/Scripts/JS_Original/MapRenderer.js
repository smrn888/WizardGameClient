/**
 * MapRenderer.js - با سیستم انیمیشن کامل
 */

class MapRenderer {
  constructor(canvas, mapData) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d');
    this.mapData = mapData;
    this.tileSize = mapData.tileSize;
    
    this.zoomLevel = 0.6;
    
    this.camera = { x: 0, y: 0, targetX: 0, targetY: 0 };
    this.smoothing = 0.12;
    
    this.images = {};
    this.characterImagePaths = {
      player: '/gryffindor front.png',
      slytherin: '/slytherin.png',
      ravenclaw: '/ravenclaw.png',
      deatheater: '/deatheater.png',
      dementor: '/dementor.png',
      patronus: '/patronus.png',
      hufflepuff: '/hufflepuff.png',
      'fall_slytherin': 'slytherin fall.png',

      // انیمیشن‌های جدید پلیر:
      'walk_left': 'walking left-Sheet.png', 
      'walk_right': 'walking Right.png', 
      'magic_left': 'magicLeft.png',
      'magic_right': 'magicRightt.png',
      'walk_up': 'walking back.png',
      'walk_down': 'grifindor front walking.png',
      'stand_up': 'grifindor back.png',
      'stand_down': 'gryffindor front.png'


    };

    // تعریف اطلاعات فریم انیمیشن
    this.animationData = {
      frameWidth: 64,
      frameHeight: 64,
      walk_left: { frames: 8, fps: 12 },
      walk_right: { frames: 8, fps: 12 },
      walk_up: { frames: 8, fps: 12 },
      walk_down: { frames: 8, fps: 12 },
      magic_left: { frames: 8, fps: 10 },
      magic_right: { frames: 8, fps: 10 },
      fall_slytherin: { frames: 8, fps: 10 },

    };

    this.imagesLoaded = false;
    this.layers = { floor: null, walls: null };
    this.time = 0;
    this.currentZone = null;
    
    this.minimapCanvas = document.createElement('canvas');
    this.minimapCtx = this.minimapCanvas.getContext('2d');
    
    this.init();
  }

  async init() {
    if (this.mapData.minimap?.enabled) {
      this.minimapCanvas.width = 200;
      this.minimapCanvas.height = 140;
    }
    
    await this.loadImages();
    
    if (this.imagesLoaded) {
      this.prerenderLayers();
    }
  }

  loadImages() {
    const imagePaths = { ...this.characterImagePaths };
    
    this.mapData.zones.forEach(zone => {
      if (zone.texture) {
        imagePaths[zone.id] = `/${zone.texture}`;
      }
    });
    
    const promises = Object.entries(imagePaths).map(([key, path]) => {
      return new Promise((resolve) => {
        const img = new Image();
        img.onload = () => {
          this.images[key] = img;
          resolve();
        };
        img.onerror = () => {
          console.warn(`❌ Failed to load: ${path}`);
          this.images[key] = null;
          resolve();
        };
        img.src = path;
      });
    });
    
    return Promise.all(promises).then(() => {
      this.imagesLoaded = true;
      console.log('✅ All assets loaded');
    });
  }

  prerenderLayers() {
    const w = this.mapData.width * this.tileSize;
    const h = this.mapData.height * this.tileSize;
    
    this.layers.floor = this.createLayer(w, h);
    const floorCtx = this.layers.floor.getContext('2d');
    this.renderFloor(floorCtx);
    
    this.layers.walls = this.createLayer(w, h);
    const wallsCtx = this.layers.walls.getContext('2d');
    this.renderWalls(wallsCtx);
  }
  
  createLayer(width, height) {
    const layer = document.createElement('canvas');
    layer.width = width;
    layer.height = height;
    return layer;
  }

  renderFloor(ctx) {
    const { width, height, tileSize } = this.mapData;
    
    ctx.fillStyle = '#000000';
    ctx.fillRect(0, 0, width * tileSize, height * tileSize);

    this.mapData.zones.forEach(zone => {
      const img = this.images[zone.id];
      const { x, y, width: w, height: h } = zone.bounds;
      
      if (img) {
        ctx.drawImage(
          img,
          x * tileSize,
          y * tileSize,
          w * tileSize,
          h * tileSize
        );
      } else {
        ctx.fillStyle = zone.lighting?.tint || '#4a4a4a';
        ctx.fillRect(
          x * tileSize,
          y * tileSize,
          w * tileSize,
          h * tileSize
        );
      }
    });
  }

  renderWalls(ctx) {
    const { tileSize } = this.mapData;
    
    this.mapData.zones.forEach(zone => {
      const { x, y, width: w, height: h } = zone.bounds;
      
      ctx.strokeStyle = '#000000';
      ctx.lineWidth = 8;
      ctx.strokeRect(
        x * tileSize,
        y * tileSize,
        w * tileSize,
        h * tileSize
      );

      if (zone.exits) {
        zone.exits.forEach(exit => {
          const doorSize = 3;
          const doorStart = exit.door_at ?? Math.floor(
            (exit.side === 'north' || exit.side === 'south' ? w : h) / 2 - doorSize / 2
          );

          let px = 0, py = 0, dw = 0, dh = 0;

          if (exit.side === 'north') {
            px = (x + doorStart) * tileSize;
            py = y * tileSize;
            dw = doorSize * tileSize;
            dh = tileSize;
          } else if (exit.side === 'south') {
            px = (x + doorStart) * tileSize;
            py = (y + h - 1) * tileSize;
            dw = doorSize * tileSize;
            dh = tileSize;
          } else if (exit.side === 'west') {
            px = x * tileSize;
            py = (y + doorStart) * tileSize;
            dw = tileSize;
            dh = doorSize * tileSize;
          } else if (exit.side === 'east') {
            px = (x + w - 1) * tileSize;
            py = (y + doorStart) * tileSize;
            dw = tileSize;
            dh = doorSize * tileSize;
          }

          ctx.clearRect(px, py, dw, dh);
          
          ctx.fillStyle = 'rgba(255, 215, 0, 0.15)';
          ctx.fillRect(px, py, dw, dh);
          
          ctx.strokeStyle = '#FFD700';
          ctx.lineWidth = 3;
          ctx.strokeRect(px, py, dw, dh);
        });
      }
    });
  }

  getZoneAt(x, y) {
    const tileX = Math.floor(x / this.tileSize);
    const tileY = Math.floor(y / this.tileSize);
    
    return this.mapData.zones.find(zone => {
      return tileX >= zone.bounds.x && 
             tileX < zone.bounds.x + zone.bounds.width &&
             tileY >= zone.bounds.y && 
             tileY < zone.bounds.y + zone.bounds.height;
    });
  }

  updateCamera(worldX, worldY) {
    this.camera.targetX = worldX;
    this.camera.targetY = worldY;
    
    const viewWidth = this.canvas.width / this.zoomLevel;
    const viewHeight = this.canvas.height / this.zoomLevel;
    
    const maxX = this.mapData.width * this.tileSize - viewWidth / 2;
    const maxY = this.mapData.height * this.tileSize - viewHeight / 2;
    const minX = viewWidth / 2;
    const minY = viewHeight / 2;
    
    this.camera.targetX = Math.max(minX, Math.min(this.camera.targetX, maxX));
    this.camera.targetY = Math.max(minY, Math.min(this.camera.targetY, maxY));
    
    this.camera.x += (this.camera.targetX - this.camera.x) * this.smoothing;
    this.camera.y += (this.camera.targetY - this.camera.y) * this.smoothing;
  }
  
  centerCamera(worldX, worldY) {
    this.camera.x = worldX;
    this.camera.y = worldY;
    this.camera.targetX = worldX;
    this.camera.targetY = worldY;
  }

  checkDoorTransition(worldX, worldY, playerRadius) {
    if (!this.mapData) return null;
    const { tileSize } = this.mapData;
  
    const activeZone = this.currentZone || this.getZoneAt(worldX, worldY);
    if (!activeZone || !activeZone.exits) return null;
  
    const bounds = activeZone.bounds;
    const doorSize = 3;
    const threshold = tileSize * 1.2;
  
    for (const exit of activeZone.exits) {
      const { side, connects_to, door_at } = exit;
      if (!side || !connects_to) continue;
  
      const doorStart = door_at ?? Math.floor(
        (side === 'north' || side === 'south' ? bounds.width : bounds.height) / 2 - doorSize / 2
      );
  
      let doorPixelXMin, doorPixelXMax, doorPixelYMin, doorPixelYMax;
  
      if (side === 'north') {
        doorPixelXMin = (bounds.x + doorStart) * tileSize;
        doorPixelXMax = (bounds.x + doorStart + doorSize) * tileSize;
        doorPixelYMin = (bounds.y * tileSize) - threshold;
        doorPixelYMax = bounds.y * tileSize + threshold;
      } else if (side === 'south') {
        doorPixelXMin = (bounds.x + doorStart) * tileSize;
        doorPixelXMax = (bounds.x + doorStart + doorSize) * tileSize;
        doorPixelYMin = (bounds.y + bounds.height) * tileSize - threshold;
        doorPixelYMax = (bounds.y + bounds.height) * tileSize + threshold;
      } else if (side === 'west') {
        doorPixelXMin = bounds.x * tileSize - threshold;
        doorPixelXMax = bounds.x * tileSize + threshold;
        doorPixelYMin = (bounds.y + doorStart) * tileSize;
        doorPixelYMax = (bounds.y + doorStart + doorSize) * tileSize;
      } else if (side === 'east') {
        doorPixelXMin = (bounds.x + bounds.width) * tileSize - threshold;
        doorPixelXMax = (bounds.x + bounds.width) * tileSize + threshold;
        doorPixelYMin = (bounds.y + doorStart) * tileSize;
        doorPixelYMax = (bounds.y + doorStart + doorSize) * tileSize;
      } else {
        continue;
      }
  
      if (
        worldX >= doorPixelXMin &&
        worldX <= doorPixelXMax &&
        worldY >= doorPixelYMin &&
        worldY <= doorPixelYMax
      ) {
        return {
          currentSide: side,
          targetZoneId: connects_to,
          currentZone: activeZone,
        };
      }
    }
  
    return null;
  }

  checkCollision(x, y, radius) {
    const zone = this.getZoneAt(x, y);
    if (!zone) return true;
    
    const { bounds } = zone;
    const tileX = Math.floor(x / this.tileSize);
    const tileY = Math.floor(y / this.tileSize);
    
    const margin = 2;
    const innerBounds = {
      xMin: bounds.x + margin,
      xMax: bounds.x + bounds.width - margin - 1,
      yMin: bounds.y + margin,
      yMax: bounds.y + bounds.height - margin - 1
    };
    
    if (tileX >= innerBounds.xMin && tileX <= innerBounds.xMax &&
        tileY >= innerBounds.yMin && tileY <= innerBounds.yMax) {
      return false;
    }
    
    const onBorder = 
      tileX < innerBounds.xMin || tileX > innerBounds.xMax ||
      tileY < innerBounds.yMin || tileY > innerBounds.yMax;
    
    if (!onBorder) return false;
    
    if (zone.exits) {
      for (let exit of zone.exits) {
        if (this.isPointOnDoor(tileX, tileY, bounds, exit)) {
          return false;
        }
      }
    }
    
    return true;
  }

  isPointOnDoor(tileX, tileY, bounds, exit) {
    const doorSize = 3;
    const doorStart = exit.door_at ?? Math.floor(
      (exit.side === 'north' || exit.side === 'south' 
        ? bounds.width 
        : bounds.height) / 2 - doorSize / 2
    );
    const doorEnd = doorStart + doorSize;
    
    const margin = 1;
    
    switch(exit.side) {
      case 'north':
        return tileY >= bounds.y - margin && tileY <= bounds.y + margin && 
               tileX >= bounds.x + doorStart - margin && 
               tileX < bounds.x + doorEnd + margin;
      
      case 'south':
        return tileY >= bounds.y + bounds.height - 1 - margin && 
               tileY <= bounds.y + bounds.height - 1 + margin &&
               tileX >= bounds.x + doorStart - margin && 
               tileX < bounds.x + doorEnd + margin;
      
      case 'west':
        return tileX >= bounds.x - margin && tileX <= bounds.x + margin && 
               tileY >= bounds.y + doorStart - margin && 
               tileY < bounds.y + doorEnd + margin;
      
      case 'east':
        return tileX >= bounds.x + bounds.width - 1 - margin && 
               tileX <= bounds.x + bounds.width - 1 + margin &&
               tileY >= bounds.y + doorStart - margin && 
               tileY < bounds.y + doorEnd + margin;
      
      default:
        return false;
    }
  }

  render(playerX, playerY, allEntities, player) {
    if (!this.imagesLoaded) {
      this.ctx.fillStyle = '#111';
      this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
      this.ctx.fillStyle = '#FFD700';
      this.ctx.font = '24px Arial';
      this.ctx.textAlign = 'center';
      this.ctx.fillText(
        'Loading Hogwarts...',
        this.canvas.width / 2,
        this.canvas.height / 2
      );
      return;
    }

    this.updateCamera(playerX, playerY);
    
    const zone = this.getZoneAt(playerX, playerY);
    if (zone && zone !== this.currentZone) {
      this.currentZone = zone;
      console.log(`🏰 Entered: ${zone.name}`);
    }

    this.ctx.save();
    
    this.ctx.scale(this.zoomLevel, this.zoomLevel);
    
    this.ctx.translate(
      Math.round((this.canvas.width / this.zoomLevel) / 2 - this.camera.x),
      Math.round((this.canvas.height / this.zoomLevel) / 2 - this.camera.y)
    );
    
    if (this.layers.floor) {
      this.ctx.drawImage(this.layers.floor, 0, 0);
    }

    if (this.layers.walls) {
      this.ctx.drawImage(this.layers.walls, 0, 0);
    }
    
    const renderOrder = [...allEntities].sort((a, b) => {
      return (a.y || 0) - (b.y || 0);
    });

    renderOrder.forEach(entity => {
      const x = entity.x;
      const y = entity.y;

      if (entity.type === 'spell') {
        if (entity.source === 'player' && entity.damage === 0 && entity.radius > 20) {
          this.drawPatronus(x, y, entity.radius);
        } else {
          this.drawSpell(entity);
        }
      } else if (entity.house === 'dementor' || entity.type === 'dementor') {
        this.drawDementor(x, y, entity.radius || 30);
      } else if (entity.name) {
        const isPlayer = entity.type === 'player';
        this.drawCharacter(
          x, y, entity.radius || 30,
          entity.name, entity.house, entity.health, entity.maxHealth || 100, 
          isPlayer, entity
        );
      }
    });

    this.ctx.restore();
    
    const visibleEnemies = allEntities.filter(e => 
      e.name && e.name !== player.name && e.currentZoneId === player.currentZoneId
    );
    this.renderMinimap(playerX, playerY, visibleEnemies);
    this.renderZoneInfo();
  }

  drawCharacter(x, y, radius, name, house, health, maxHealth, isPlayer, entity) {
    // سایه زیر کاراکتر
    this.ctx.fillStyle = 'rgba(0, 0, 0, 0.3)';
    this.ctx.beginPath();
    this.ctx.ellipse(x, y + radius * 0.2, radius * 0.7, radius * 0.2, 0, 0, Math.PI * 2);
    this.ctx.fill();
  
    const size = radius * 2;
  
    // ✨ سیستم انیمیشن برای پلیر
    if (isPlayer && entity) {
      let animKey = null;
  
      // تعیین کلید انیمیشن مناسب بر اساس جهت و وضعیت
      if (entity.isCasting) {
        animKey = entity.facing === 'right' ? 'magic_right' :
                  entity.facing === 'left'  ? 'magic_left'  :
                  entity.facing === 'up'    ? 'stand_up'    :
                  entity.facing === 'down'  ? 'stand_down'  :
                  null;
      } 
      else if (entity.isMoving) {
        animKey = entity.facing === 'right' ? 'walk_right' :
                  entity.facing === 'left'  ? 'walk_left'  :
                  entity.facing === 'up'    ? 'walk_up'    :
                  entity.facing === 'down'  ? 'walk_down'  :
                  null;
      }
  
      // اگر انیمیشن فعال است (حرکت یا طلسم)
      if (animKey && this.images[animKey]) {
        const animImg = this.images[animKey];
        const animInfo = this.animationData[animKey];
  
        if (animImg && animInfo) {
          const fw = this.animationData.frameWidth;
          const fh = this.animationData.frameHeight;
          const frame = entity.frame || 0;
  
          this.ctx.drawImage(
            animImg,
            frame * fw, 0,      // محل فریم در اسپریت‌شیت
            fw, fh,             // اندازه فریم در اسپریت‌شیت
            x - size / 2, y - size / 2, // محل رسم
            size, size          // اندازه نهایی
          );
        } else {
          // fallback به تصویر پیش‌فرض
          const fallbackImg = this.images['player'];
          if (fallbackImg) {
            this.ctx.drawImage(fallbackImg, x - size / 2, y - size / 2, size, size);
          }
        }
      } 
      else {
        // حالت Idle (ایستاده)
        let idleKey = 'player';
        if (entity.facing === 'up' && this.images['stand_up']) {
          idleKey = 'stand_up';
        } else if (entity.facing === 'down' && this.images['stand_down']) {
          idleKey = 'stand_down';
        }
  
        const idleImg = this.images[idleKey];
        if (idleImg) {
          this.ctx.drawImage(idleImg, x - size / 2, y - size / 2, size, size);
        }
      }
    } 
    else {
      // سایر کاراکترها (دشمنان و غیره)
      const imageKey = house;
      const img = this.images[imageKey];
  
      if (img) {
        this.ctx.drawImage(img, x - size / 2, y - size / 2, size, size);
      } else {
        this.ctx.fillStyle = '#888';
        this.ctx.beginPath();
        this.ctx.arc(x, y, radius, 0, Math.PI * 2);
        this.ctx.fill();
      }
// ✅ انیمیشن افتادن Slytherin
    if (house === 'slytherin' && entity.isFalling) {
      const img = this.images['fall_slytherin'];
      const animInfo = this.animationData['fall_slytherin'];

      if (img && animInfo) {
        const fw = this.animationData.frameWidth;
        const fh = this.animationData.frameHeight;
        const currentFrame = Math.floor(entity.fallFrame);
        const frame = Math.min(currentFrame, animInfo.frames - 1);

        // 🔹 محاسبه موقعیت پرت شدن (فقط یک بار در هر فریم)
        if (!entity.fallVelocity) {
          entity.fallVelocity = { x: 0, y: 0 };
        }
        
        // افزایش سرعت پرت شدن به تدریج
        if (entity.fallDirX !== undefined && entity.fallDirY !== undefined) {
          entity.fallVelocity.x = entity.fallDirX * 3; // سرعت ثابت پرت شدن
          entity.fallVelocity.y = entity.fallDirY * 3;
          
          entity.x += entity.fallVelocity.x;
          entity.y += entity.fallVelocity.y;
        }

        // ✨ رسم فریم فعلی (با flip در صورت نیاز)
        this.ctx.save();
        
        // flip کردن تصویر اگر به سمت چپ پرت شده
        if (entity.fallFlip) {
          this.ctx.translate(entity.x, entity.y);
          this.ctx.scale(-1, 1);
          this.ctx.drawImage(
            img,
            frame * fw, 0, fw, fh,
            -radius, -radius,
            radius * 2, radius * 2
          );
        } else {
          this.ctx.drawImage(
            img,
            frame * fw, 0, fw, fh,
            entity.x - radius, entity.y - radius,
            radius * 2, radius * 2
          );
        }
        
        this.ctx.restore();

        // 🎞️ حرکت به فریم بعدی
        entity.fallFrame += 0.2; // سرعت انیمیشن

        // 🧹 اتمام انیمیشن
        if (entity.fallFrame >= animInfo.frames) {
          entity.health = 0;
          entity.isFalling = false;
        }

        return; // خروج از تابع - انیمیشن عادی نشان داده نمی‌شود
      }
    }
      
      
    }
  
    // 🔴 نوار سلامتی
    const barWidth = radius * 1.5;
    const barHeight = 5;
    const healthRatio = Math.max(0, health / maxHealth);
  
    this.ctx.fillStyle = '#222';
    this.ctx.fillRect(x - barWidth / 2, y - radius - 15, barWidth, barHeight);
    
    this.ctx.fillStyle = healthRatio > 0.5 ? '#4CAF50' : healthRatio > 0.2 ? '#FFA500' : '#FF0000';
    this.ctx.fillRect(x - barWidth / 2, y - radius - 15, barWidth * healthRatio, barHeight);
  
    // 🪶 نمایش نام کاراکتر
    this.ctx.fillStyle = isPlayer ? '#FFD700' : '#FFF';
    this.ctx.font = 'bold 12px Arial';
    this.ctx.textAlign = 'center';
    this.ctx.strokeStyle = '#000';
    this.ctx.lineWidth = 3;
    this.ctx.strokeText(name.split(' ')[0], x, y - radius - 20);
    this.ctx.fillText(name.split(' ')[0], x, y - radius - 20);
  }
  
  drawSpell(spell) {
    const { x, y, sourceX, sourceY, color, vx, vy } = spell;
    this.ctx.save();
  
    // نقطه شروع و پایان پرتو
    const startX = sourceX ?? x - vx * 10;
    const startY = sourceY ?? y - vy * 10;
    const endX = x;
    const endY = y;
  
    // ✨ تنظیم درخشش پرتو
    this.ctx.shadowColor = color;
    this.ctx.shadowBlur = 25;
    this.ctx.globalAlpha = 0.9;
  
    // پرتو اصلی
    this.ctx.strokeStyle = color;
    this.ctx.lineWidth = 6;
    this.ctx.beginPath();
    this.ctx.moveTo(startX, startY);
    this.ctx.lineTo(endX, endY);
    this.ctx.stroke();
  
    // لایه سفید درخشان وسط
    this.ctx.strokeStyle = '#ffffff';
    this.ctx.globalAlpha = 0.7;
    this.ctx.lineWidth = 2;
    this.ctx.beginPath();
    this.ctx.moveTo(startX, startY);
    this.ctx.lineTo(endX, endY);
    this.ctx.stroke();
  
    // جرقه‌های ریز در طول مسیر (افکت جادویی)
    const sparkCount = 2;
    for (let i = 0; i < sparkCount; i++) {
      const t = Math.random();
      const sx = startX + (endX - startX) * t + (Math.random() - 0.5) * 10;
      const sy = startY + (endY - startY) * t + (Math.random() - 0.5) * 10;
      this.ctx.globalAlpha = 0.5 + Math.random() * 0.4;
      this.ctx.fillStyle = color;
      this.ctx.beginPath();
      this.ctx.arc(sx, sy, 2 + Math.random() * 2, 0, Math.PI * 2);
      this.ctx.fill();
    }
  
    this.ctx.restore();
  }
  
  

  drawPatronus(x, y, radius) {
    const img = this.images['patronus'];
    
    this.ctx.save();
    this.ctx.globalAlpha = 0.75 + Math.sin(this.time * 0.05) * 0.25;
    this.ctx.shadowColor = '#99ccff';
    this.ctx.shadowBlur = 30;

    if (img) {
      const size = radius * 2.5;
      this.ctx.drawImage(img, x - size / 2, y - size / 2, size, size);
    } else {
      this.ctx.fillStyle = '#99ccff';
      this.ctx.beginPath();
      this.ctx.arc(x, y, radius, 0, Math.PI * 2);
      this.ctx.fill();
      
      this.ctx.globalAlpha = 0.5;
      this.ctx.fillStyle = '#ffffff';
      this.ctx.beginPath();
      this.ctx.arc(x, y, radius * 0.6, 0, Math.PI * 2);
      this.ctx.fill();
    }
    
    this.ctx.restore();
  }

  drawDementor(x, y, radius) {
    const img = this.images['dementor'];
    
    this.ctx.save();
    this.ctx.globalAlpha = 0.85;
    this.ctx.shadowColor = '#000';
    this.ctx.shadowBlur = 20;
    
    if (img) {
      const size = radius * 2.2;
      this.ctx.drawImage(img, x - size / 2, y - size / 2, size, size);
    } else {
      this.ctx.fillStyle = '#000';
      this.ctx.beginPath();
      this.ctx.arc(x, y, radius, 0, Math.PI * 2);
      this.ctx.fill();
    }
    
    this.ctx.restore();
  }

  renderMinimap(playerX, playerY, enemies) {
    if (!this.mapData.minimap?.enabled) return;
    
    const ctx = this.minimapCtx;
    const ts = this.tileSize;
    const scale = 0.08;
    
    ctx.clearRect(0, 0, 200, 140);
    
    ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
    ctx.fillRect(0, 0, 200, 140);
    
    this.mapData.zones.forEach(zone => {
      const { x, y, width, height } = zone.bounds;
      
      ctx.fillStyle = zone === this.currentZone ? 'rgba(255, 215, 0, 0.2)' : 'rgba(100, 100, 100, 0.3)';
      ctx.fillRect(
        x * ts * scale,
        y * ts * scale,
        width * ts * scale,
        height * ts * scale
      );
      
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.4)';
      ctx.lineWidth = 1;
      ctx.strokeRect(
        x * ts * scale,
        y * ts * scale,
        width * ts * scale,
        height * ts * scale
      );
    });
    
    ctx.fillStyle = '#00FF00';
    ctx.shadowColor = '#00FF00';
    ctx.shadowBlur = 5;
    ctx.beginPath();
    ctx.arc(playerX * scale, playerY * scale, 3, 0, Math.PI * 2);
    ctx.fill();
    ctx.shadowBlur = 0;
    
    enemies.forEach(enemy => {
      const ex = enemy.x * scale;
      const ey = enemy.y * scale;
      
      ctx.fillStyle = '#FF0000';
      ctx.beginPath();
      ctx.arc(ex, ey, 2, 0, Math.PI * 2);
      ctx.fill();
    });

    this.ctx.drawImage(this.minimapCanvas, this.canvas.width - 220, 20);
    
    this.ctx.strokeStyle = '#FFD700';
    this.ctx.lineWidth = 2;
    this.ctx.strokeRect(this.canvas.width - 220, 20, 200, 140);
  }

  renderZoneInfo() {
    if (!this.currentZone) return;
    
    const padding = 15;
    const boxWidth = 320;
    const boxHeight = 70;
    
    this.ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
    this.ctx.fillRect(
      padding,
      this.canvas.height - boxHeight - padding,
      boxWidth,
      boxHeight
    );
    
    this.ctx.strokeStyle = '#FFD700';
    this.ctx.lineWidth = 2;
    this.ctx.strokeRect(
      padding,
      this.canvas.height - boxHeight - padding,
      boxWidth,
      boxHeight
    );
    
    this.ctx.fillStyle = '#FFD700';
    this.ctx.font = 'bold 20px Arial';
    this.ctx.textAlign = 'left';
    this.ctx.fillText(
      this.currentZone.name,
      padding + 15,
      this.canvas.height - boxHeight - padding + 30
    );
    
    this.ctx.fillStyle = '#FFFFFF';
    this.ctx.font = '14px Arial';
    this.ctx.fillText(
      this.currentZone.description,
      padding + 15,
      this.canvas.height - boxHeight - padding + 52
    );
  }

  screenToWorld(screenX, screenY) {
    return {
      x: (screenX / this.zoomLevel) + this.camera.x - (this.canvas.width / this.zoomLevel) / 2,
      y: (screenY / this.zoomLevel) + this.camera.y - (this.canvas.height / this.zoomLevel) / 2
    };
  }

  worldToScreen(worldX, worldY) {
    return {
      x: (worldX - this.camera.x + (this.canvas.width / this.zoomLevel) / 2) * this.zoomLevel,
      y: (worldY - this.camera.y + (this.canvas.height / this.zoomLevel) / 2) * this.zoomLevel
    };
  }

  update() {
    this.time++;
  }
}

export default MapRenderer;