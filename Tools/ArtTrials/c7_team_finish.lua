-- Aseprite source authoring. Preserve AI building geometry and broad planes;
-- no roof extraction or replacement facade, no runtime image transformations.
local root=assert(app.params.root);local pc=app.pixelColor
local function color(h) return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255) end
local function save(s,n) s:saveAs(root..'/Native/'..n..'.aseprite');s:saveCopyAs(root..'/Native/'..n..'.png');s:close() end
for _,name in ipairs({'thatch','tile-house'}) do
 local s=app.open(root..'/Prepared/'..name..'.png');s.layers[1].name='Figma teammate style / whole AI geometry / native contour'
 local im=s.cels[1].image;local edits={}
 for y=0,47 do for x=0,63 do
  if pc.rgbaA(im:getPixel(x,y))>0 then
   local edge=false
   for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do local xx,yy=x+d[1],y+d[2];if xx<0 or yy<0 or xx>63 or yy>47 or pc.rgbaA(im:getPixel(xx,yy))==0 then edge=true end end
   if edge then table.insert(edits,{x,y}) end
  end
 end end
 for _,p in ipairs(edits) do im:drawPixel(p[1],p[2],color('2e241d')) end
 save(s,name)
end
local baseGrass,lightGrass,darkGrass=color('8aab4d'),color('b2cc73'),color('6d8b3a')
local baseEarth,lightEarth,darkEarth=color('c4a277'),color('e2c496'),color('a2835e')
local depth={2,2,1,1,2,2,3,3,2,1,1,2,3,3,2,2}
local function has(m,b) return (m & b)~=0 end
local function isGrass(m,x,y)
 local n,e,s,w=has(m,1),has(m,2),has(m,4),has(m,8)
 if not n and y<depth[x+1] then return false end;if not s and 15-y<depth[16-x] then return false end
 if not w and x<depth[16-y] then return false end;if not e and 15-x<depth[y+1] then return false end
 for _,c in ipairs({{n and e,16,15-x,y},{e and s,32,15-x,15-y},{s and w,64,x,15-y},{w and n,128,x,y}}) do
  if c[1] and not has(m,c[2]) and c[3]^2+c[4]^2<10 then return false end
 end
 return true
end
-- Source-designed sparse 2-3px motifs, as in the observed team materials.
local groups={{{3,4},{11,10}},{{8,3},{4,11}},{{11,5},{6,12}},{{4,7},{12,12}}}
local function ground(name,mask,variant,earthOnly)
 local s=Sprite(16,16,ColorMode.RGB);s.layers[1].name='Team-style terrain / native 2-3px motifs / complete piece'
 local im=s.cels[1].image
 for y=0,15 do for x=0,15 do im:drawPixel(x,y,not earthOnly and isGrass(mask,x,y) and baseGrass or baseEarth) end end
 for _,p in ipairs(groups[variant+1]) do
  local x,y=p[1],p[2]
  if earthOnly then
   im:drawPixel(x,y,darkEarth);im:drawPixel(x+1,y,lightEarth)
  elseif isGrass(mask,x,y) and isGrass(mask,x+1,y+2) then
   im:drawPixel(x,y,lightGrass);im:drawPixel(x,y+1,darkGrass);im:drawPixel(x+1,y+1,lightGrass);im:drawPixel(x+1,y+2,darkGrass)
  end
 end
 save(s,name)
end
for mask=0,255 do
 local n,e,s,w=has(mask,1),has(mask,2),has(mask,4),has(mask,8)
 if (not has(mask,16) or n and e) and (not has(mask,32) or e and s) and (not has(mask,64) or s and w) and (not has(mask,128) or w and n) then ground('grass-'..mask,mask,0,false) end
end
ground('dirt',0,0,true)
for v=1,3 do ground('grass-center-'..v,255,v,false);ground('dirt-'..v,0,v,true) end
print('Whole houses kept, 47 boundary masks + 6 center variants; pine left untouched')
