"""Preserve the supplied horror characters' original rigs, textures and motion.

Connection map: each existing skinned body remains bound to its original bones;
the lowest animated vertex meets the road in Unity's grounded Run clip. No new
geometry is assembled. High and low meshes share the same skeleton and atlas.
"""
import bpy, pathlib, sys, json
from mathutils import Vector, Matrix

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'Tools'))
from prepare_meshy import mobile_bake, simplify, select, bounds

OUT = ROOT / 'Assets/TsilaRun/Art/Meshy/Source'
entries = []
for stem, name in [('horror_girl', 'HorrorGirl'), ('horror_skunx', 'HorrorSkunx')]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(ROOT / 'Assets/TsilaRun/Art/AI' / (stem + '.glb')))
    rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    action = next(a for a in bpy.data.actions if not a.name.startswith('Key'))
    rig.animation_data_clear()
    for bone in rig.pose.bones: bone.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and any(m.type == 'ARMATURE' for m in o.modifiers)]
    for obj in meshes:
        world = obj.matrix_world.copy()
        obj.parent = None
        obj.matrix_world = world
    # glTF may include inspection helper meshes which are not part of the skin.
    for obj in list(bpy.context.scene.objects):
        if obj.type == 'MESH' and obj not in meshes: bpy.data.objects.remove(obj, do_unlink=True)
    world = rig.matrix_world.copy(); rig.parent = None; rig.matrix_world = world
    points = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
    lo = Vector([min(p[i] for p in points) for i in range(3)])
    hi = Vector([max(p[i] for p in points) for i in range(3)])
    print('HORROR_REST_BOUNDS', name, list(lo), list(hi), flush=True)
    source_count = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    scale = 1.8 / (hi.z - lo.z)
    normalizer = Matrix.Diagonal((scale, scale, scale, 1)) @ Matrix.Translation(Vector((-(lo.x + hi.x)/2, -(lo.y + hi.y)/2, -lo.z)))
    rig.matrix_world = normalizer @ rig.matrix_world
    for obj in meshes:
        obj.matrix_world = normalizer @ obj.matrix_world
        select(obj)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # Bake one mobile atlas while preserving vertex groups and the supplied rig.
    obj = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.select_all(action='DESELECT')
        for mesh in meshes: mesh.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.join()
    obj.name = name + '_Mesh'
    mobile_bake(obj, name, 16000)
    mat = obj.data.materials[0]; mat.name = name + '_Body'
    for image in bpy.data.images:
        if image.name in (name + '_BaseColor', name + '_Normal'):
            image.filepath_raw = str(OUT / 'Textures' / (image.name + '.png'))
            image.file_format = 'PNG'; image.save()
    # Keep the rig's original transform; applying its scale would change the
    # units of the authored animation translations and break the motion.
    obj.parent = rig; obj.matrix_parent_inverse = rig.matrix_world.inverted()
    rig.name = name + '_Armature'
    action.name = name + '_Run'
    rig.animation_data_create(); rig.animation_data.action = action
    bpy.context.scene.render.fps = 30
    bpy.context.scene.frame_start = int(action.frame_range[0])
    bpy.context.scene.frame_end = int(action.frame_range[1])
    select(obj); rig.select_set(True)
    def export(suffix, animation):
        bpy.ops.export_scene.fbx(filepath=str(OUT / (name + suffix + '.fbx')), use_selection=True,
            object_types={'MESH', 'ARMATURE'}, axis_forward='-Z', axis_up='Y',
            apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False,
            bake_anim=animation, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
            bake_anim_simplify_factor=0.0, path_mode='RELATIVE')
    export('', True)
    high_count = sum(len(p.vertices)-2 for p in obj.data.polygons)
    prepared_lo, prepared_hi = bounds(obj)
    rig.animation_data_clear()
    for bone in rig.pose.bones: bone.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    simplify(obj, 6000)
    rig.select_set(True)
    export('_LOD1', False)
    entries.append(dict(name=name, input=stem+'.glb', sourceTriangles=source_count,
        triangles=high_count, lodTriangles=sum(len(p.vertices)-2 for p in obj.data.polygons),
        rigged=True, originalRig=True, boundsMin=list(prepared_lo), boundsMax=list(prepared_hi),
        textures={kind: 'Assets/TsilaRun/Art/Meshy/Source/Textures/'+name+'_'+kind+'.png' for kind in ('BaseColor','Normal')}))
    print('HORROR_PREPARED', name, high_count, entries[-1]['lodTriangles'], flush=True)
manifest = OUT.parent / 'Manifest.json'
previous = json.loads(manifest.read_text())
updated = {e['name']:e for e in entries}
manifest.write_text(json.dumps([updated.pop(e['name'],e) for e in previous]+list(updated.values()), indent=2))
print('TSILA_HORROR_PREPARE_OK', flush=True)
