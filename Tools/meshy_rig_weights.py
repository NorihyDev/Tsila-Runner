"""Continuous arm/body blending for the two supplied neutral poses."""
from mathutils import Vector

def bind_smooth(obj,rig,name):
    deform=[b for b in rig.data.bones if b.name!='root']
    obj.vertex_groups.clear()
    groups={b.name:obj.vertex_groups.new(name=b.name) for b in deform}
    def distance(p,b):
        d=b.tail_local-b.head_local
        t=max(0,min(1,(p-b.head_local).dot(d)/d.length_squared))
        return (p-b.head_local-t*d).length
    def nearest(p,candidates):
        near=sorted((distance(p,b),b.name) for b in candidates)[:3]
        weights=[1/max(d,.025)**4 for d,n in near];total=sum(weights)
        return {n:w/total for (_,n),w in zip(near,weights)}
    for v in obj.data.vertices:
        p=v.co;side='L' if p.x>=0 else 'R'
        body=[b for b in deform if '.' not in b.name or b.name in ['upper_leg.'+side,'lower_leg.'+side,'foot.'+side]]
        arms=[b for b in deform if b.name in ['upper_arm.'+side,'lower_arm.'+side,'hand.'+side,'shoulder.'+side]]
        limit=.20+(1.43-p.z)*(.39 if name=='Tsila' else .18)
        t=max(0,min(1,(abs(p.x)-(limit-.05))/.10)) if .68<p.z<1.47 else 0
        mask=t*t*(3-2*t)
        weights={n:w*(1-mask) for n,w in nearest(p,body).items()}
        for n,w in nearest(p,arms).items():weights[n]=weights.get(n,0)+w*mask
        top=sorted(weights.items(),key=lambda x:-x[1])[:4];total=sum(w for n,w in top)
        for n,w in top:
            if w>0:groups[n].add([v.index],w/total,'REPLACE')
