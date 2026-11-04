-- Offline source editing only. Wool: preserve the original except the three notch pixels.
local root=app.params.repo;local folder=root..'/ArtReview/ItemIcons-v3-20261104/';local old=root..'/ArtReview/E3-E2-Art-20261103/Native/';local pc=app.pixelColor
local wool=app.open(old..'transit_wool.aseprite');local im=wool.cels[1].image
im:drawPixel(7,3,pc.rgba(48,34,26,255));im:drawPixel(8,3,pc.rgba(48,34,26,255));im:drawPixel(8,4,pc.rgba(248,234,202,255))
wool:saveAs(folder..'Native/transit_wool.aseprite');wool:saveAs(folder..'Native/transit_wool.png');wool:close()
local spice=app.open(old..'transit_spice.aseprite');spice:saveAs(folder..'Native/transit_spice.aseprite');spice:saveAs(folder..'Native/transit_spice.png');spice:close()
local palette={{48,34,26},{93,64,38},{137,101,60},{173,134,83},{206,169,112},{233,207,160},{248,234,202},{255,249,231},{185,66,32},{219,108,43},{165,111,42},{211,161,67},{243,202,101}}
local function nearest(c)
 local best,dist=nil,1e20
 for _,v in ipairs(palette)do local d=(pc.rgbaR(c)-v[1])^2+(pc.rgbaG(c)-v[2])^2+(pc.rgbaB(c)-v[3])^2;if d<dist then best=v;dist=d end end
 return pc.rgba(best[1],best[2],best[3],255)
end
local src=app.open(folder..'Raw/grain.png');local si=src.cels[1].image;local x0,y0,x1,y1=si.width,si.height,0,0
for p in si:pixels()do if pc.rgbaA(p())>=224 then x0=math.min(x0,p.x);y0=math.min(y0,p.y);x1=math.max(x1,p.x);y1=math.max(y1,p.y)end end
local bw,bh=x1-x0+1,y1-y0+1;local scale=math.min(14/bw,14/bh);local w,h=math.max(1,math.floor(bw*scale)),math.max(1,math.floor(bh*scale));local ox,oy=math.floor((16-w)/2),15-h
local dest=Sprite(16,16,ColorMode.RGB);local di=dest.cels[1].image
for y=0,h-1 do for x=0,w-1 do local c=si:getPixel(x0+math.min(bw-1,math.floor((x+.5)*bw/w)),y0+math.min(bh-1,math.floor((y+.5)*bh/h)));if pc.rgbaA(c)>=128 then di:drawPixel(ox+x,oy+y,nearest(c))end end end
dest:saveAs(folder..'Native/grain.aseprite');dest:saveAs(folder..'Native/grain.png');dest:close();src:close()
