"""Keep Punky's supplied head and UVs; attach it to Tsila's running outfit."""
# Connection map (Blender Z up): Tsila neck ends at 1.475m; Punky neck
# begins at 1.450m, giving 25mm overlap. Shoes rest at Z=0. Original GLB
# and facial texture stay untouched. Both meshes share Tsila's skeleton.
import bpy, bmesh, pathlib, sys, math
from mathutils import Vector, Matrix

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Tools'))
from prepare_meshy import select, simplify, add_rig, bounds

OUT = ROOT / 'Assets/TsilaRun/Art/Meshy/Source'
TEX = OUT / 'Textures'

def clip_z(obj, height, keep_above):
    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
        dist=0.00001, plane_co=(0, 0, height), plane_no=(0, 0, 1),
        clear_inner=keep_above, clear_outer=not keep_above)
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context='VERTS')
    bm.to_mesh(obj.data); bm.free(); obj.data.update()

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(ROOT / 'Assets/TsilaRun/Art/AI/Punky.glb'))
head = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
head.name = 'Punky_OriginalHead'
world = head.matrix_world.copy(); head.parent = None; head.matrix_world = world
select(head); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# The supplied bust faces +X; the donor runner faces -Y in Blender.
rotation = Matrix.Rotation(-math.pi / 2, 4, 'Z')
for vertex in head.data.vertices: vertex.co = rotation @ vertex.co
clip_z(head, .16, True)
lo, hi = bounds(head)
head_scale = .35 / (hi.z - lo.z)
center_x = (lo.x + hi.x) * .5
center_y = (lo.y + hi.y) * .5
for vertex in head.data.vertices:
    vertex.co = Vector(((vertex.co.x-center_x)*head_scale,
        (vertex.co.y-center_y)*head_scale, 1.45+(vertex.co.z-lo.z)*head_scale))
head.data.update()
face_material = head.data.materials[0]
face_material.name = 'Punky_FaceDetails'
shader = next(n for n in face_material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
image = shader.inputs['Base Color'].links[0].from_node.image
image.scale(2048, 2048)
image.filepath_raw = str(TEX / 'Punky_BaseColor.png'); image.file_format = 'PNG'; image.save()
normal_socket = shader.inputs['Normal']
if normal_socket.is_linked:
    normal_node = normal_socket.links[0].from_node
    if normal_node.type == 'NORMAL_MAP' and normal_node.inputs['Color'].is_linked:
        normal_image = normal_node.inputs['Color'].links[0].from_node.image
        normal_image.scale(1024, 1024)
        normal_image.filepath_raw = str(TEX / 'Punky_Normal.png'); normal_image.file_format = 'PNG'; normal_image.save()
simplify(head, 12000)
print('PUNKY_HEAD_BOUNDS', tuple(bounds(head)[0]), tuple(bounds(head)[1]), flush=True)

bpy.ops.import_scene.fbx(filepath=str(OUT / 'Tsila.fbx'), use_anim=False)
body = next(o for o in bpy.context.scene.objects if o.type == 'MESH' and o != head)
world = body.matrix_world.copy(); body.parent = None; body.matrix_world = world
body.modifiers.clear()
select(body); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
clip_z(body, 1.475, False)
for material in body.data.materials: material.name = 'Punky_Body'
simplify(body, 10000)
for rig in list(bpy.context.scene.objects):
    if rig.type == 'ARMATURE': bpy.data.objects.remove(rig, do_unlink=True)
for action in list(bpy.data.actions): bpy.data.actions.remove(action)
select(body); head.select_set(True); bpy.ops.object.join()
body.name = 'Punky_Mesh'
rig = add_rig(body, 'Tsila')
rig.name = 'Punky_Armature'; rig.data.name = 'Punky_Skeleton'
for action in bpy.data.actions:
    if action.name.startswith('Tsila_'): action.name = action.name.replace('Tsila_', 'Punky_', 1)
# Keep eyes, lips, nose and hair rigidly attached to the head bone.
for vertex in body.data.vertices:
    if vertex.co.z >= 1.50:
        for group in body.vertex_groups: group.remove([vertex.index])
        body.vertex_groups['head'].add([vertex.index], 1.0, 'REPLACE')
select(body); rig.select_set(True)
bpy.context.scene.render.fps = 24
def export(suffix, animation):
    bpy.ops.export_scene.fbx(filepath=str(OUT / ('Punky'+suffix+'.fbx')), use_selection=True,
        object_types={'MESH','ARMATURE'}, axis_forward='-Z', axis_up='Y',
        apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False, bake_anim=animation,
        bake_anim_use_all_actions=animation, bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=0.0, path_mode='RELATIVE')
export('', True)
print('PUNKY_HIGH_TRIANGLES', sum(len(p.vertices)-2 for p in body.data.polygons), flush=True)
simplify(body, 8000); select(body); rig.select_set(True); export('_LOD1', False)
print('PUNKY_PREPARED_OK', flush=True)

# Reference render of the assembled neutral pose, using the actual facial texture.
for bone in rig.pose.bones: bone.matrix_basis = Matrix.Identity(4)
rig.animation_data_clear(); bpy.context.view_layer.update()
scene = bpy.context.scene; scene.render.engine = 'CYCLES'; scene.cycles.samples = 12
scene.world = bpy.data.worlds.new('Punky Preview World'); scene.world.use_nodes = True
scene.world.node_tree.nodes.get('Background').inputs[0].default_value = (.16,.18,.22,1)
for pos,power in [((3,-4,5),750),((-3,-2,3),500)]:
    data=bpy.data.lights.new('Preview Light','AREA'); data.energy=power; data.size=4
    lamp=bpy.data.objects.new('Preview Light',data); scene.collection.objects.link(lamp)
    lamp.location=pos; lamp.rotation_euler=(Vector((0,0,.9))-lamp.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Preview Camera'); camera=bpy.data.objects.new('Preview Camera',data)
scene.collection.objects.link(camera); camera.location=(2,-5,2.4)
camera.rotation_euler=(Vector((0,0,.9))-camera.location).to_track_quat('-Z','Y').to_euler()
data.type='ORTHO'; data.ortho_scale=2.2; scene.camera=camera
scene.render.resolution_x=512; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.filepath=str(ROOT / 'Logs/MeshyInspection/Punky-assembled.png')
bpy.ops.render.render(write_still=True)
