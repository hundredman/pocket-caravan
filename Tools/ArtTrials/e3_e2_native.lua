-- Offline native-art preparation only; no production textures/maps are written.
local root=app.params.repo
local folder=root..'/ArtReview/E3-E2-Art-20261103/'
local pc=app.pixelColor
local function color(r,g,b,a)return pc.rgba(r,g,b,a or 255)end
local wood={{46,34,27},{76,49,33},{107,71,43},{145,97,57},{180,129,75},{211,163,105},{236,199,143},{248,222,173}}
local icons={{48,34,26},{93,64,38},{137,101,60},{173,134,83},{206,169,112},{233,207,160},{248,234,202},{255,249,231},{185,66,32},{219,108,43}}
local function nearest(c,palette)
 local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c);local best,dist=nil,1e20
 for _,v in ipairs(palette)do local d=(r-v[1])^2+(g-v[2])^2+(b-v[3])^2;if d<dist then best=v;dist=d end end
 return color(best[1],best[2],best[3])
end
local function save(s,name)
 s:saveAs(folder..'Native/'..name..'.aseprite');s:saveAs(folder..'Native/'..name..'.png');s:close()
end
local function object(name,w,h,palette)
 local src=app.open(folder..'Raw/'..name..'.png');local im=src.cels[1].image
 local minx,miny,maxx,maxy=im.width,im.height,0,0
 for it in im:pixels()do if pc.rgbaA(it())>=224 then minx=math.min(minx,it.x);miny=math.min(miny,it.y);maxx=math.max(maxx,it.x);maxy=math.max(maxy,it.y)end end
 local bw,bh=maxx-minx+1,maxy-miny+1;local scale=math.min((w-2)/bw,(h-2)/bh)
 local dw,dh=math.max(1,math.floor(bw*scale)),math.max(1,math.floor(bh*scale))
 local ox,oy=math.floor((w-dw)/2),h-1-dh
 local out=Sprite(w,h,ColorMode.RGB);local dest=out.cels[1].image
 for y=0,dh-1 do for x=0,dw-1 do
  local sx=minx+math.min(bw-1,math.floor((x+.5)*bw/dw));local sy=miny+math.min(bh-1,math.floor((y+.5)*bh/dh));local c=im:getPixel(sx,sy)
  if pc.rgbaA(c)>=128 then dest:drawPixel(ox+x,oy+y,nearest(c,palette))end
 end end
 save(out,name);src:close()
end
object('fallen_log',48,48,wood)
object('transit_spice',16,16,icons)
object('transit_wool',16,16,icons)
-- V2: generated contour, widened offline on the native grid. Only east port is open.
local src=app.open(folder..'v2/Raw/river_flood_high.png');local im=src.cels[1].image
local left={}
for y=3,44 do
 local sy=math.floor((y-3+.5)*im.height/42);local first=79
 for x=0,79 do
  local c=im:getPixel(math.min(im.width-1,math.floor((x+.5)*im.width/80)),math.min(im.height-1,sy))
  if pc.rgbaA(c)>=128 and pc.rgbaG(c)>pc.rgbaR(c)+15 and pc.rgbaB(c)>pc.rgbaR(c)+10 then first=x;break end
 end
 left[y]=math.max(4,math.floor(first*.40)+4)
end
left[3]=70;left[4]=60;left[43]=60;left[44]=70
local function flood(name,low)
 local mask={}
 for y=3,44 do
  local start=left[y];if low then start=math.min(76,start+34)end
  for x=start,79 do mask[y*80+x]=true end
 end
 local function has(x,y)return mask[y*80+x]and x>=0 and x<80 and y>=0 and y<48 end
 local out=Sprite(80,48,ColorMode.RGB);local dest=out.cels[1].image
 for y=1,46 do for x=1,79 do
  local inside=has(x,y);local near=false;local far=false;local edge=false
  for dy=-2,2 do for dx=-2,2 do
   if math.abs(dx)+math.abs(dy)<=2 then
    if has(x+dx,y+dy)then far=true;if math.abs(dx)+math.abs(dy)<=1 then near=true end
    elseif x+dx<80 and math.abs(dx)+math.abs(dy)<=1 then edge=true end
   end
  end end
  if inside then dest:drawPixel(x,y,edge and color(120,160,153)or color(127,185,175))
  elseif near then dest:drawPixel(x,y,color(255,255,255))
  elseif far then dest:drawPixel(x,y,color(132,103,94))end
 end end
 -- An actual open east water connection; no bank across the join.
 for y=5,42 do dest:drawPixel(79,y,color(127,185,175))end
 for _,v in ipairs({{24,18,3},{39,32,4},{60,13,3},{68,37,3}})do
  for x=v[1],v[1]+v[3]-1 do if has(x,v[2])and has(x,v[2]-2)and has(x-2,v[2])then dest:drawPixel(x,v[2],color(172,214,202))end end
 end
 save(out,name)
end
flood('river_flood_high',false);flood('river_flood_low',true);src:close()
