-- Offline whole-icon conversion to the existing 16px grid and explicit item palette.
local root=app.params.repo;local folder=root..'/ArtReview/ItemIcons-v2-20261104/';local pc=app.pixelColor
local palette={{48,34,26},{93,64,38},{137,101,60},{173,134,83},{206,169,112},{233,207,160},{248,234,202},{255,249,231},{185,66,32},{219,108,43},{165,111,42},{211,161,67},{243,202,101}}
local function nearest(c)
 local best,dist=nil,1e20
 for _,v in ipairs(palette)do local d=(pc.rgbaR(c)-v[1])^2+(pc.rgbaG(c)-v[2])^2+(pc.rgbaB(c)-v[3])^2;if d<dist then best=v;dist=d end end
 return pc.rgba(best[1],best[2],best[3],255)
end
for _,v in ipairs({{'grain',12,14},{'transit_spice',12,14},{'transit_wool',14,12}})do
 local n,mw,mh=v[1],v[2],v[3];local src=app.open(folder..'Raw/'..n..'.png');local im=src.cels[1].image
 local x0,y0,x1,y1=im.width,im.height,0,0
 for p in im:pixels()do if pc.rgbaA(p())>=224 then x0=math.min(x0,p.x);y0=math.min(y0,p.y);x1=math.max(x1,p.x);y1=math.max(y1,p.y)end end
 local bw,bh=x1-x0+1,y1-y0+1;local scale=math.min(mw/bw,mh/bh);local w,h=math.max(1,math.floor(bw*scale)),math.max(1,math.floor(bh*scale));local ox,oy=math.floor((16-w)/2),15-h
 local dest=Sprite(16,16,ColorMode.RGB);local di=dest.cels[1].image
 for y=0,h-1 do for x=0,w-1 do local c=im:getPixel(x0+math.min(bw-1,math.floor((x+.5)*bw/w)),y0+math.min(bh-1,math.floor((y+.5)*bh/h)));if pc.rgbaA(c)>=128 then di:drawPixel(ox+x,oy+y,nearest(c))end end end
 dest:saveAs(folder..'Native/'..n..'.aseprite');dest:saveAs(folder..'Native/'..n..'.png');dest:close();src:close()
end
