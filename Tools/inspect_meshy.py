import bpy, json, pathlib, math
from mathutils import Vector

root = pathlib.Path(__file__).resolve().parents[1]
out = root / 'Logs/MeshyInspection'
out.mkdir(parents=True, exist_ok=True)
report = {}
for path in sorted((root / 'Assets/TsilaRun/Art/AI').glob('*.glb')):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
    lo = Vector(tuple(min(p[i] for p in points) for i in range(3)))
    hi = Vector(tuple(max(p[i] for p in points) for i in range(3)))
    report[path.stem] = {'min':list(lo), 'max':list(hi), 'objects':[(o.name,len(o.data.vertices),len(o.data.polygons)) for o in meshes], 'materials': [m.name for m in bpy.data.materials], 'images':[(i.name,list(i.size)) for i in bpy.data.images]}
    center = (lo+hi)*.5
    size = max(hi-lo)
    for o in meshes:
        o.matrix_world.translation -= center
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 8
    scene.world = bpy.data.worlds.new('Inspection World')
    scene.world.use_nodes = True
    next(n for n in scene.world.node_tree.nodes if n.type == 'BACKGROUND').inputs[0].default_value = (.2,.2,.2,1)
    for pos, power in [((2,-3,4),800),((-3,-1,2),500)]:
        data=bpy.data.lights.new('Inspection Light','AREA'); data.energy=power; data.shape='DISK'; data.size=size*2
        light=bpy.data.objects.new('Inspection Light',data); scene.collection.objects.link(light); light.location=Vector(pos)*size
        light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('Inspection Camera'); camera=bpy.data.objects.new('Inspection Camera',data); scene.collection.objects.link(camera)
    camera.location=Vector((2,-4,2.1))*size; camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler(); data.type='ORTHO'; data.ortho_scale=size*1.35; scene.camera=camera
    scene.render.resolution_x=384; scene.render.resolution_y=384; scene.render.resolution_percentage=100
    scene.render.filepath=str(out/(path.stem+'.png')); bpy.ops.render.render(write_still=True)
    print('INSPECTED',path.stem,report[path.stem],flush=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'Assets/TsilaRun/BlenderPack/Source/Tsila.fbx'))
report['template']={'armatures':[{ 'name':o.name, 'bones':[(b.name,list(b.head_local),list(b.tail_local)) for b in o.data.bones]} for o in bpy.context.scene.objects if o.type=='ARMATURE'], 'actions':[a.name for a in bpy.data.actions]}
(out/'report.json').write_text(json.dumps(report,indent=2))
