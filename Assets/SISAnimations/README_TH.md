# SIS Rigged Physics — แอนิเมชันสำหรับเกม

ตัวละครเป้าหมาย: `Assets/SISStylized/Models/SIS_Rigged_Physics.vrm`

เปิดเมนู **Tools > SIS Animations > Create preview and controller** เพื่อสร้างหรืออัปเดตไฟล์ จากนั้นเปิด **SIS_AnimationPreview.unity** ในโฟลเดอร์นี้ กด Play และเลือกท่าจากปุ่มด้านซ้าย

## ท่าทั้งหมด 27 ท่า

- เคลื่อนที่: Idle, Idle_LookAround, Walk, Run, WalkBackward, StrafeLeft, StrafeRight
- นั่ง: SitDown, Sit, StandUp
- นอน: LayDown, LieIdle, Sleep, GetUp
- ย่อ: CrouchDown, CrouchIdle, CrouchWalk, CrouchUp
- กระโดด: Jump, Fall, Land
- การกระทำ: Wave, Talk, Cheer, Interact, PickUp, HitReact

SitDown ต่อเข้า Sit, LayDown ต่อเข้า Sleep, CrouchDown ต่อเข้า CrouchIdle โดยอัตโนมัติ ท่าเล่นครั้งเดียวอื่น ๆ กลับ Idle เมื่อจบ สามารถแก้ลำดับใน Animator Controller ได้

## ใช้ในเกม

- ใช้ Prefab **SIS_Animated.prefab** ที่มีโมเดล VRM พร้อม Animator Controller และฟิสิกส์เดิม
- ไฟล์ **Clips/*.anim** เป็น Humanoid clips แยกท่า ใช้กับ Animator ได้
- เรียก `animator.CrossFadeInFixedTime("Walk", 0.18f);` เพื่อเปลี่ยนท่า หรือใช้ `SISAnimationPreview.Play` ในฉากตัวอย่าง
- เป็นการเคลื่อนไหวแบบ in-place: เกมต้องขยับและหมุนตัวละครเอง โดยปิด Apply Root Motion
- ความเร็วเริ่มต้นที่แนะนำให้ลองจับจังหวะ: Walk 1 เมตร/วินาที, Run 3 เมตร/วินาที ต้องปรับตาม Character Controller ของเกม
- Jump มีการยกสะโพกสำหรับแสดงท่ากระโดด หากเกมขยับตัวละครแนวดิ่งด้วยฟิสิกส์ ควรปรับเส้นโค้งสะโพกให้เข้ากับระบบเพื่อไม่ให้ยกซ้ำ
- Spring Bone ของ `Vrm10Instance` ทำงาน LateUpdate หลัง Animator; FBX มีเฉพาะโครงกระดูก Humanoid และบรรพบุรุษ ไม่ใส่คีย์ผม/เสื้อผ้าทับฟิสิกส์
- ปิดระบบ webcam/body capture ที่เขียนกระดูกเดียวกันขณะใช้ Animator
- ท่า Sleep ปิดตาผ่านสคริปต์ฉากตัวอย่าง หากนำเฉพาะคลิปไปใช้ในเกม ให้ควบคุม VRM expression `blink` เพิ่มเอง

## ขอบเขตและการปรับแต่ง

ชุดนี้เป็นแอนิเมชันพื้นฐานที่สร้างให้ SIS โดยตรง ไม่ใช่ motion capture ท่านั่งอ้างอิงเบาะสูงประมาณ 0.4 เมตร ท่านอนอ้างอิงพื้น ต้องปรับตำแหน่งเข้ากับเก้าอี้หรือเตียงจริง และเพิ่ม Foot IK หากใช้พื้นที่ต่างระดับ การสัมผัสพื้น มือหยิบวัตถุ และการชนเสื้อผ้าต้องตรวจในบริบทของเกมอีกครั้ง

ต้นฉบับ Blender: `ArtSource/SIS_Animations/SIS_GameAnimations.blend` เลือก Action จาก Action Editor มี 27 ท่า ใช้กระดูก Humanoid โดยตรงในไฟล์สำเนานี้ ต้นฉบับ rig และ VRM ไม่ถูกแก้ไข ฟิสิกส์ถูกปิดในไฟล์แอนิเมชัน Blender เพื่อให้แก้ท่าได้คงที่ แต่เปิดใช้งานตามเดิมบน VRM ใน Unity

เมนูสร้างจะอัปเดตไฟล์ที่สร้างภายในโฟลเดอร์นี้ หากแก้ Controller/Prefab/คลิปเอง ให้ทำสำเนาก่อนกดสร้างใหม่
