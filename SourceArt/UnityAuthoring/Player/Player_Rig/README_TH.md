# Player Rig

ต้นฉบับ: E:/NONAMEGAME MODEL/player.vrm — เก็บไฟล์เดิมไว้โดยไม่แก้ไข

- **Player_Rigged.blend**: ไฟล์หลักสำหรับสร้างท่าและแอนิเมชัน
- **Player_Rigged_Physics.vrm**: Humanoid พร้อมรูปหน้าและ Spring Bone สำหรับ Unity/UniVRM
- **Player_Physics_Demo.blend**: ตัวอย่างการขยับร่างกาย ใบหน้า และฟิสิกส์ 3 วินาที กด Space เพื่อเล่น
- สำเนา VRM ในโปรเจกต์: Assets/PlayerRig/Player_Rigged_Physics.vrm

## Blender

เลือก Player_Rig แล้วกด N เปิดแท็บ **Player Rig** ซึ่งติดตั้งให้แล้ว

1. เข้า Pose Mode เพื่อขยับกระดูก CTRL_ แบบ FK
2. เปลี่ยน IK Hand/Foot ของข้างที่ต้องการเป็น 1 แล้วขยับกระดูก IK_ ใช้ POLE_ ควบคุมทิศศอก/เข่า
3. ปรับ Eyes, Eyebrows และ Mouth / vowels ในแผง Player Rig มีตัวควบคุมใบหน้า 21 ช่อง พร้อม Eye Yaw/Pitch
4. เปิด Enable animation แล้วเล่น Timeline เพื่อคำนวณผมและแขนเสื้อ ปุ่ม Reset spring simulation ใช้เริ่มการจำลองใหม่
5. Reset face และ Reset body pose คืนท่าตั้งต้น

ไฟล์หลักเปิดฟิสิกส์ไว้ ส่วนไฟล์ Demo ใช้คีย์ที่บันทึกจากการจำลองและปิดการจำลองสดเพื่อไม่ให้ขยับซ้ำ

## Unity

ใช้ Player_Rigged_Physics.vrm ที่อยู่ใน Assets/PlayerRig กับ UniVRM 1.0 ที่มีในโปรเจกต์ ตัวควบคุม FK/IK ของ Blender ไม่ได้ส่งเข้า VRM; Unity ใช้ Humanoid และระบบ IK ของเกมเอง ส่วน morph ใบหน้าและ Spring Bone ส่งเข้าไฟล์ VRM แล้ว

ฟิสิกส์ครอบคลุมผม 10 ชุดและแขนเสื้อ 12 ชุด มี collider รวม 34 ตัว ฟิสิกส์นี้ใช้กระดูก ไม่ใช่ cloth simulation ทั้งเสื้อ กางเกง หรือเนกไท จึงไม่มีการสร้างรอยยับหรือ self-collision ของผ้า

## ตรวจสอบแล้ว

- Blender rig รวม 200 กระดูก รวมตัวควบคุม
- Humanoid เดิม 54 รายการยังอยู่ครบ
- ใบหน้า 21 ช่องผ่านการตรวจ driver และการเปลี่ยนรูปร่างจริง
- IK มือและเท้า 4 จุดคลาดเคลื่อนประมาณ 0.000022 เมตรในการทดสอบ
- จำลอง 90 เฟรม: ผมขยับสูงสุดประมาณ 15 องศา แขนเสื้อประมาณ 12.7 องศา
- นำ VRM ที่ส่งออกกลับเข้า Blender ผ่าน: 22 spring chains, 14 custom expressions, 72 shape keys รวม Basis
- ยังไม่ได้ตรวจการเล่นจริงใน Unity สำหรับตัวละครใหม่นี้
