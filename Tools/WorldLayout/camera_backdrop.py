"""Visual-only exterior cells needed to frame internal scene seams at the screen edge."""
def exterior_cells(objects,width,height,targets,view_width=48,view_height=32):
    left=right=top=bottom=0
    for o in objects:
        if o['kind']!='exit'or o['target']not in targets:continue
        x,y,w,h=o['x'],o['y'],o.get('w',1),o.get('h',1)
        if o['edge']=='E':left=max(left,view_width-x-w)
        elif o['edge']=='W':right=max(right,x+view_width-width)
        elif o['edge']=='N':bottom=max(bottom,y+view_height-height)
        elif o['edge']=='S':top=max(top,view_height-y-h)
    return [(x,y)for y in range(-top,height+bottom)for x in range(-left,width+right)if not(0<=x<width and 0<=y<height)]
