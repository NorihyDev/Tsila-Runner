"""Convert the supplied GLBs in an isolated Blender process; originals stay intact."""
# Connection map: imported characters are single continuous meshes, bound to a
# connected root/pelvis/spine/head, shoulder/arm/hand and hip/knee/foot skeleton.
# Road modules share measured ends at +/-12m; no newly assembled primitives.
import bpy, bmesh, pathlib, math, json, sys
from mathutils import Vector, Quaternion, Matrix
from collections import Counter

ROOT=pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Tools'))
from meshy_rig_weights import bind_smooth
OUT=ROOT/'Assets/TsilaRun/Art/Meshy/Source'
OUT.mkdir(parents=True, exist_ok=True)
TEX=OUT/'Textures'; TEX.mkdir(exist_ok=True)
REPORT=[]
SPECS={
 'tsila':('Tsila',20000,None), 'officier':('Officer',20000,None),
 'road':('RoadSection',5000,(8,24,.36)),
 'border':('Curb',1200,(.2,24,.16)),
 'barriere':('Tower',1800,(1.8,1.1,3.6)),
 'obstacle-slide':('Overhead',1200,(2.24,.9,2.8)),
 'coins':('Coin',1000,(.6,.2,.6)),
 'aimant':('CoinMagnet',1800,(.65,.25,.65)),
 'arbre-tropical':('Tree',4500,None), 'palmier':('Palm',5500,None),
 'house':('House',6000,(3.5,5,4.425)), 'immeuble':('Building',6500,None)}

def select(obj):
 bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj

def bounds(obj):
 vs=[v.co for v in obj.data.vertices]
 lo=Vector([min(v[i] for v in vs) for i in range(3)])
 hi=Vector([max(v[i] for v in vs) for i in range(3)])
 return lo,hi

def simplify(obj,budget):
 select(obj)
 count=sum(len(p.vertices)-2 for p in obj.data.polygons)
 if count>budget:
  mod=obj.modifiers.new('Mobile triangle budget','DECIMATE'); mod.ratio=budget/count; mod.use_collapse_triangulate=True
  bpy.ops.object.modifier_apply(modifier=mod.name)
 print('MOBILE_MESH',obj.name,count,'->',sum(len(p.vertices)-2 for p in obj.data.polygons),flush=True)
 return count

def mobile_bake(obj,name,budget):
 """Project original Meshy shading onto fresh low-mesh UVs, retaining glTF mappings."""
 high=obj.copy(); high.data=obj.data.copy(); bpy.context.collection.objects.link(high); high.name=name+'_BakeSource'
 # glTF duplicates vertices at UV/normal seams. Weld the low mesh before reducing
 # and unwrapping so its new atlas has continuous islands rather than tiny fragments.
 bm=bmesh.new();bm.from_mesh(obj.data)
 bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
 bm.to_mesh(obj.data);bm.free();obj.data.update()
 original=simplify(obj,budget)
 select(obj); bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
 bpy.ops.uv.smart_project(angle_limit=math.radians(80),island_margin=.015)
 bpy.ops.object.mode_set(mode='OBJECT')
 resolution=2048 if name in ('Tsila','Officer') else 1024
 mat=bpy.data.materials.new(name+'_Baked'); mat.use_nodes=True
 shader=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 diffuse=bpy.data.images.new(name+'_BaseColor',width=resolution,height=resolution,alpha=False)
 image_node=mat.node_tree.nodes.new('ShaderNodeTexImage');image_node.image=diffuse
 mat.node_tree.links.new(image_node.outputs['Color'],shader.inputs['Base Color']); mat.node_tree.nodes.active=image_node
 obj.data.materials.clear();obj.data.materials.append(mat)
 for p in obj.data.polygons:p.material_index=0
 # Bake emission from the original color socket so metallic assets retain their color too.
 restore=[]
 for source in high.data.materials:
  bsdf=next(n for n in source.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
  output=next(n for n in source.node_tree.nodes if n.type=='OUTPUT_MATERIAL')
  emit=source.node_tree.nodes.new('ShaderNodeEmission')
  base=bsdf.inputs['Base Color']
  if base.is_linked:source.node_tree.links.new(base.links[0].from_socket,emit.inputs['Color'])
  else:emit.inputs['Color'].default_value=base.default_value
  source.node_tree.links.new(emit.outputs[0],output.inputs['Surface'])
  restore.append((source,bsdf,output,emit))
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=1
 scene.render.bake.use_selected_to_active=True;scene.render.bake.cage_extrusion=.045;scene.render.bake.max_ray_distance=.1;scene.render.bake.margin=12
 select(obj);high.select_set(True)
 bpy.ops.object.bake(type='EMIT')
 for source,bsdf,output,emit in restore:
  source.node_tree.links.new(bsdf.outputs[0],output.inputs['Surface']);source.node_tree.nodes.remove(emit)
 normal=bpy.data.images.new(name+'_Normal',width=1024,height=1024,alpha=False)
 normal.colorspace_settings.name='Non-Color'
 normal_tex=mat.node_tree.nodes.new('ShaderNodeTexImage');normal_tex.image=normal;mat.node_tree.nodes.active=normal_tex
 scene.render.bake.normal_space='TANGENT'
 bpy.ops.object.bake(type='NORMAL')
 normal_node=mat.node_tree.nodes.new('ShaderNodeNormalMap');mat.node_tree.links.new(normal_tex.outputs['Color'],normal_node.inputs['Color']);mat.node_tree.links.new(normal_node.outputs[0],shader.inputs['Normal'])
 bpy.data.objects.remove(high,do_unlink=True)
 print('BAKED_MOBILE_TEXTURES',name,flush=True)
 return original

def textures(obj,name):
 original=obj.data.materials[0]
 shader=next(n for n in original.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 slots={}
 base=shader.inputs['Base Color']
 if base.is_linked and base.links[0].from_node.type=='TEX_IMAGE': slots['BaseColor']=base.links[0].from_node.image
 normal=shader.inputs['Normal']
 if normal.is_linked:
  node=normal.links[0].from_node
  if node.type=='NORMAL_MAP' and node.inputs['Color'].is_linked: slots['Normal']=node.inputs['Color'].links[0].from_node.image
 for kind,img in slots.items():
  limit=2048 if name in ('Tsila','Officer') and kind=='BaseColor' else 1024
  if max(img.size)>limit: img.scale(limit,limit)
  img.filepath_raw=str(TEX/(name+'_'+kind+'.png')); img.file_format='PNG'; img.save()
 original.name=name+'_Body' if name in ('Tsila','Officer') else name+'_Material'
 if name in ('Tsila','Officer'):
  details=original.copy(); details.name=name+'_FaceDetails'; obj.data.materials.append(details)
  for p in obj.data.polygons:
   c=p.center
   if c.z>1.46 or c.z<.18 or (abs(c.x)>.29 and c.z<.88): p.material_index=1
 return {k:str(v.filepath_raw) for k,v in slots.items()}

def add_rig(obj,name):
 data=bpy.data.armatures.new(name+'_Skeleton'); rig=bpy.data.objects.new(name+'_Armature',data); bpy.context.collection.objects.link(rig)
 select(rig); bpy.ops.object.mode_set(mode='EDIT')
 bones={}
 def bone(n,a,b,parent=None,deform=True):
  e=data.edit_bones.new(n); e.head=a; e.tail=b; e.use_deform=deform
  if parent: e.parent=bones[parent]
  bones[n]=e
 bone('root',(0,0,0),(0,0,.2),None,False)
 bone('pelvis',(0,0,.87),(0,0,1.03),'root')
 bone('spine',(0,0,1.03),(0,0,1.22),'pelvis')
 bone('chest',(0,0,1.22),(0,0,1.43),'spine')
 bone('neck',(0,0,1.43),(0,0,1.55),'chest')
 bone('head',(0,0,1.55),(0,0,1.78),'neck')
 for suffix,s in [('L',1),('R',-1)]:
  elbow=(s*(.36 if name=='Tsila' else .24),0,1.10)
  wrist=(s*(.47 if name=='Tsila' else .30),-.025,.86)
  bone('shoulder.'+suffix,(s*.04,0,1.43),(s*.19,0,1.43),'chest')
  bone('upper_arm.'+suffix,(s*.19,0,1.43),elbow,'shoulder.'+suffix)
  bone('lower_arm.'+suffix,elbow,wrist,'upper_arm.'+suffix)
  bone('hand.'+suffix,wrist,(wrist[0]+s*.035,-.04,.75),'lower_arm.'+suffix)
  bone('upper_leg.'+suffix,(s*.105,0,.87),(s*.13,0,.49),'pelvis')
  bone('lower_leg.'+suffix,(s*.13,0,.49),(s*.15,0,.13),'upper_leg.'+suffix)
  bone('foot.'+suffix,(s*.15,0,.13),(s*.15,-.19,.07),'lower_leg.'+suffix)
 bpy.ops.object.mode_set(mode='OBJECT')
 bind_smooth(obj,rig,name)
 obj.parent=rig; mod=obj.modifiers.new('Meshy skeletal animation','ARMATURE'); mod.object=rig
 rig.rotation_mode='QUATERNION'
 for b in rig.pose.bones: b.rotation_mode='QUATERNION'
 def rotate_global(n,axis,angle):
  b=rig.pose.bones[n]; rest=b.bone.matrix_local.to_quaternion(); b.rotation_quaternion=rest.inverted()@Quaternion(axis,angle)@rest
 for state,frames in [('Idle',48),('Run',24),('Jump',24),('Slide',24)]:
  action=bpy.data.actions.new(name+'_'+state); rig.animation_data_create(); rig.animation_data.action=action
  for f in range(1,frames+2):
   phase=(f-1)/frames*2*math.pi
   for b in rig.pose.bones: b.location=(0,0,0); b.rotation_quaternion=Quaternion()
   for suffix,s in [('L',1),('R',-1)]:
    upper=data.bones['upper_arm.'+suffix]; direction=(upper.tail_local-upper.head_local).normalized()
    # Rest arms descend diagonally; bring them near the sides while preserving elbows.
    close=Quaternion((0,1,0),s*math.atan2(abs(direction.x),-direction.z)*.8)
    rest=upper.matrix_local.to_quaternion()
    rig.pose.bones[upper.name].rotation_quaternion=rest.inverted()@close@rest
    if state=='Run':
     swing=math.sin(phase)*s
     rotate_global('upper_leg.'+suffix,(1,0,0),swing*.60)
     rotate_global('lower_leg.'+suffix,(1,0,0),-max(0,-swing)*.95)
     rig.pose.bones[upper.name].rotation_quaternion=rest.inverted()@Quaternion((1,0,0),-swing*.48)@close@rest
     rotate_global('lower_arm.'+suffix,(1,0,0),.60)
    elif state=='Jump':
     rotate_global('upper_leg.'+suffix,(1,0,0),.35*s)
     rotate_global('lower_leg.'+suffix,(1,0,0),-.45)
     rotate_global('lower_arm.'+suffix,(1,0,0),.65)
    elif state=='Slide':
     rotate_global('lower_arm.'+suffix,(1,0,0),.20)
   root=rig.pose.bones['root']
   lift=.015*(1+math.sin(phase)) if state=='Idle' else .035*(1-math.cos(phase*2)) if state=='Run' else 0
   if state=='Slide': root.rotation_quaternion=Quaternion((1,0,0),math.pi/2); lift=.34
   root.location=root.bone.matrix_local.to_quaternion().inverted()@Vector((0,0,lift))
   for b in rig.pose.bones:
    b.keyframe_insert('location',frame=f); b.keyframe_insert('rotation_quaternion',frame=f)
  rig.animation_data.action=None
  track=rig.animation_data.nla_tracks.new(); track.name=state; track.strips.new(state,1,action); track.mute=True
 for b in rig.pose.bones: b.location=(0,0,0); b.rotation_quaternion=Quaternion()
 bpy.context.scene.frame_set(1)
 return rig

selected=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for stem,(name,budget,target) in SPECS.items():
 if selected and stem not in selected:continue
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.gltf(filepath=str(ROOT/'Assets/TsilaRun/Art/AI'/ (stem+'.glb')))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 for o in meshes:
  select(o); bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 obj=meshes[0]; obj.name=name+'_Mesh'
 if len(meshes)>1:
  bpy.ops.object.select_all(action='DESELECT')
  for o in meshes:o.select_set(True)
  bpy.context.view_layer.objects.active=obj; bpy.ops.object.join()
 original=mobile_bake(obj,name,budget)
 if stem in ('road','border'):
  # Both supplied models run along X; the game's repeated direction is Blender Y.
  for v in obj.data.vertices:v.co=Vector((-v.co.y,v.co.x,v.co.z))
 lo,hi=bounds(obj); size=hi-lo
 if name in ('Tsila','Officer'): target=tuple(size*(1.8/size.z))
 if name=='Tree':target=tuple(size*(3.95/size.z))
 if name=='Palm':target=tuple(size*(5.2/size.z))
 if name=='Building':target=tuple(size*(6.8/size.z))
 center=(lo+hi)*.5
 for v in obj.data.vertices:
  v.co=Vector(((v.co.x-center.x)*target[0]/size.x,(v.co.y-center.y)*target[1]/size.y,(v.co.z-lo.z)*target[2]/size.z))
 if name=='RoadSection':
  # Find the deck from central upward triangles, excluding raised curbs.
  obj.data.update(); levels=Counter()
  for p in obj.data.polygons:
   if p.normal.z>.85 and abs(p.center.x)<3.2: levels[round(p.center.z,2)]+=p.area
  deck=levels.most_common(1)[0][0]
  for v in obj.data.vertices:
   v.co.z-=deck
 if name=='Coin':
  for v in obj.data.vertices:v.co.z-=.3
 if name=='Overhead':
  obj.data.update()
  hit,point,normal,index=obj.ray_cast(Vector((0,0,-1)),Vector((0,0,1)))
  if not hit:raise RuntimeError('Overhead center beam was not found')
  opening=point.z
  if opening<.1:raise RuntimeError('Overhead model has no clear opening')
  for v in obj.data.vertices:
   v.co.z=v.co.z/opening if v.co.z<=opening else 1+(v.co.z-opening)*1.8/(2.8-opening)
 obj.data.update()
 tex=textures(obj,name)
 rig=add_rig(obj,name) if name in ('Tsila','Officer') else None
 select(obj)
 if rig:rig.select_set(True)
 bpy.context.scene.render.fps=24
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,bake_anim=bool(rig),bake_anim_use_all_actions=bool(rig),bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.0,path_mode='RELATIVE')
 lo,hi=bounds(obj)
 entry={'name':name,'input':stem+'.glb','sourceTriangles':original,'triangles':sum(len(p.vertices)-2 for p in obj.data.polygons),'boundsMin':list(lo),'boundsMax':list(hi),'textures':{k:str(pathlib.Path(v).relative_to(ROOT)).replace('\\','/') for k,v in tex.items()},'rigged':bool(rig)}
 if rig:
  # LOD uses the same skeleton and weights, exported without duplicated animation.
  simplify(obj,8000); select(obj); rig.select_set(True)
  bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'_LOD1.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE')
  entry['lodTriangles']=sum(len(p.vertices)-2 for p in obj.data.polygons)
 REPORT.append(entry); print('PREPARED',entry,flush=True)
manifest=OUT.parent/'Manifest.json'
if selected and manifest.exists():
 previous=json.loads(manifest.read_text()); updated={x['name']:x for x in REPORT}
 REPORT=[updated.get(x['name'],x) for x in previous]
manifest.write_text(json.dumps(REPORT,indent=2))
print('MESHY_PREPARE_OK',flush=True)
