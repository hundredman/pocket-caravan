-- Preserve the new AI architecture after A5. Do not replace it with a repeated
-- rectangle facade. All drawing is source authoring; no runtime art mutation.
local root=assert(app.params.root);local pc=app.pixelColor
local function rgb(h) return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255) end
local ink=rgb('34271f')
local function save(s,name)
 s:saveAs(root..'/Native/'..name..'.aseprite');s:saveCopyAs(root..'/Native/'..name..'.png');s:close()
end
for _,name in ipairs({'thatch','tile-house'}) do
 local s=app.open(root..'/Prepared/'..name..'.png');s.layers[1].name='AI architecture retained, A5 colors, native outline pass'
 local im=s.cels[1].image;local boundary={}
 -- Native hand-cleanup: remove isolated same-material color flecks, preserving
 -- silhouette, ink, architectural divisions and every transparent gap.
 local families={}
 for i,group in ipairs({{'b77c3e','e2ac52','f7d17b'},{'516877','8198a4','b0c1c6'},{'bfa679','ddc696','f2dfb3'},{'7d7970','a59d89','c5bba3'}}) do
  for _,h in ipairs(group) do families[rgb(h)]=i end
 end
 local changed=0
 for pass=1,2 do
  local fixes={}
  for y=1,im.height-2 do for x=1,im.width-2 do
   local c=im:getPixel(x,y);local family=families[c]
   if family then
    local counts={};local same=0
    for dy=-1,1 do for dx=-1,1 do
     if dx~=0 or dy~=0 then
      local v=im:getPixel(x+dx,y+dy)
      if families[v]==family then counts[v]=(counts[v] or 0)+1;if v==c then same=same+1 end end
     end
    end end
    if same<=1 then
     local best,num=c,0
     for v,count in pairs(counts) do if count>num or (count==num and v<best) then best,num=v,count end end
     if num>=4 and best~=c then table.insert(fixes,{x,y,best}) end
    end
   end
  end end
  for _,p in ipairs(fixes) do im:drawPixel(p[1],p[2],p[3]);changed=changed+1 end
 end
 -- Native architectural hand pass: keep the new AI roof at its sampled pixel
 -- size, move it upward within the same whole-house canvas, and DRAW a taller
 -- recessed facade. No affine stretch, repeated-row resize or map-time edits.
 local roof=Image(im)
 im:clear()
 local function rect(x0,y0,x1,y1,h)
  for yy=y0,y1 do for xx=x0,x1 do im:drawPixel(xx,yy,rgb(h)) end end
 end
 local top=name=='thatch' and 21 or 22
 rect(10,top,54,44,'bfa679');rect(11,top+3,52,42,'f2dfb3');rect(50,top+3,53,42,'ddc696')
 rect(10,43,54,46,'7d7970');rect(11,43,53,45,'a59d89');rect(11,43,53,43,'c5bba3')
 for _,x in ipairs({19,32,44}) do rect(x,44,x,45,'7d7970') end
 for _,x in ipairs({11,34,52}) do
  rect(x,top+1,x+2,42,'69452f');rect(x,top+1,x,41,'c18b52');rect(x+1,top+2,x+1,42,'9b653c')
 end
 rect(12,top+2,52,top+3,'69452f');rect(12,41,52,42,'9b653c')
 local dx=name=='thatch' and 19 or 41
 rect(dx-2,top+4,dx+12,43,'69452f');rect(dx,top+6,dx+10,42,'9b653c')
 rect(dx,top+6,dx+1,41,'c18b52');rect(dx+10,top+6,dx+10,42,'69452f')
 rect(dx+5,top+6,dx+5,41,'69452f');rect(dx+7,35,dx+7,35,'34271f')
 rect(dx-2,44,dx+12,46,'7d7970');rect(dx-2,44,dx+12,44,'c5bba3');rect(dx-1,45,dx+11,45,'a59d89')
 local wx=name=='thatch' and 39 or 15
 local ww=name=='thatch' and 10 or 17
 local wy=top+6
 rect(wx-1,wy-1,wx+ww,wy+10,'69452f');rect(wx,wy,wx+ww-1,wy+8,'ddc696');rect(wx,wy,wx+ww-2,wy+7,'f2dfb3')
 for xx=wx+3,wx+ww-2,4 do rect(xx,wy,xx,wy+8,'9b653c') end
 rect(wx,wy+4,wx+ww-1,wy+4,'9b653c');rect(wx-1,wy+10,wx+ww,wy+10,'c18b52')
 local shift,cut=name=='thatch' and 11 or 6,name=='thatch' and 32 or 28
 for yy=shift,cut do for xx=0,63 do
  local v=roof:getPixel(xx,yy)
  if pc.rgbaA(v)>0 then im:drawPixel(xx,yy-shift,v) end
 end end
 for y=0,im.height-1 do for x=0,im.width-1 do
  local c=im:getPixel(x,y)
  if pc.rgbaA(c)>0 then
   local edge=false
   for _,d in ipairs({{0,1},{0,-1},{1,0},{-1,0}}) do
    local xx,yy=x+d[1],y+d[2]
    if xx<0 or yy<0 or xx>=im.width or yy>=im.height or pc.rgbaA(im:getPixel(xx,yy))==0 then edge=true end
   end
   if edge then table.insert(boundary,{x,y}) end
  end
 end end
 for _,p in ipairs(boundary) do im:drawPixel(p[1],p[2],ink) end
 save(s,name);print('NATIVE_CLEANUP '..name..' '..changed..' isolated color pixels')
end
-- 47 full topology pieces. Quiet center materials come from the generated
-- grass/dirt source. Their narrow value range avoids camo and repeated ticks.
local source=app.open(root..'/Prepared/ground-source.png');local material=source.cels[1].image
local function has(m,b) return (m & b)~=0 end
local edgeDepth={2,2,1,1,2,2,3,3,2,1,1,2,3,3,2,2}
local function groundPixel(mask,x,y)
 local n,e,so,w=has(mask,1),has(mask,2),has(mask,4),has(mask,8)
 local grass=true
 if not n and y<edgeDepth[x+1] then grass=false end
 if not so and 15-y<edgeDepth[16-x] then grass=false end
 if not w and x<edgeDepth[16-y] then grass=false end
 if not e and 15-x<edgeDepth[y+1] then grass=false end
 -- Rounded inner corners, with common edge endpoints for neighboring pieces.
 local corners={{n and e,16,15-x,y},{e and so,32,15-x,15-y},{so and w,64,x,15-y},{w and n,128,x,y}}
 for _,c in ipairs(corners) do if c[1] and not has(mask,c[2]) and c[3]^2+c[4]^2<10 then grass=false end end
 return material:getPixel((grass and 0 or 16)+x,y)
end
for mask=0,255 do
 local n,e,so,w=has(mask,1),has(mask,2),has(mask,4),has(mask,8)
 local valid=(not has(mask,16) or (n and e)) and (not has(mask,32) or (e and so)) and (not has(mask,64) or (so and w)) and (not has(mask,128) or (w and n))
 if valid then
  local s=Sprite(16,16,ColorMode.RGB);s.layers[1].name='Complete irregular grass-dirt topology '..mask
  local im=s.cels[1].image
  for y=0,15 do for x=0,15 do im:drawPixel(x,y,groundPixel(mask,x,y)) end end
  save(s,'grass-'..mask)
 end
end
local dirt=Sprite(16,16,ColorMode.RGB);dirt.layers[1].name='Quiet AI earth material'
for y=0,15 do for x=0,15 do dirt.cels[1].image:drawPixel(x,y,material:getPixel(16+x,y)) end end
save(dirt,'dirt');source:close()
print('C7 rebuild: new complete houses, irregular 47-piece boundary kit; retained pine untouched')
